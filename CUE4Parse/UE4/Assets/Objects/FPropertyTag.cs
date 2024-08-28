using System;
using System.Collections.Generic;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Serilog;

namespace CUE4Parse.UE4.Assets.Objects;

public enum EPropertyTagSerializeType : byte
{
    /** Tag was loaded from an older version or has not yet been saved. */
    Unknown,
    /** Serialization of the property value was skipped. Tag has no value. */
    Skipped,
    /** Serialized with tagged property serialization. */
    Property,
    /** Serialized with binary or native serialization. */
    BinaryOrNative,
};

[Flags]
public enum EPropertyTagFlags : byte
{
    None = 0x00,
    HasArrayIndex = 0x01,
    HasPropertyGuid = 0x02,
    HasPropertyExtensions = 0x04,
    HasBinaryOrNativeSerialize = 0x08,
    BoolTrue = 0x10,
    SkippedSerialize = 0x20
}

[Flags]
public enum EPropertyTagExtension : byte
{
    NoExtension					= 0x00,
    ReserveForFutureUse			= 0x01, // Can be use to add a next group of extension

    ////////////////////////////////////////////////
    // First extension group
    OverridableInformation		= 0x02,

    //
    // Add more extension for the first group here
    //
}

public struct FPropertyTypeNameNode(FAssetArchive Ar)
{
    public FName Name = Ar.ReadFName();
    public int InnerCount = Ar.Read<int>();
}

public static class FPropertyTypeNameUtils
{
    public static string GetName(this Span<FPropertyTypeNameNode> nodes) => nodes.IsEmpty ? "None" : nodes[0].Name.Text;

    public static Span<FPropertyTypeNameNode> GetParameter(this Span<FPropertyTypeNameNode> nodes, int paramIndex)
    {
        if (nodes.IsEmpty) return [];
        if (paramIndex < 0 || paramIndex >= nodes[0].InnerCount) return [];

        var param = 1;
        for (int skip = paramIndex; skip > 0; --skip, ++param)
        {
            skip += nodes[param].InnerCount;
        }

        return nodes[param..];
    }
}

public class FPropertyTag
{
    public FName Name;
    public FName PropertyType;
    public int Size;
    public int ArrayIndex;
    public FPropertyTagData? TagData;
    public bool HasPropertyGuid;
    public FGuid? PropertyGuid;
    public FPropertyTagType? Tag;
    public EPropertyTagFlags PropertyTagFlags;

    public EPropertyTagSerializeType SerializeType => PropertyTagFlags.HasFlag(EPropertyTagFlags.SkippedSerialize)
            ? EPropertyTagSerializeType.Skipped
            : PropertyTagFlags.HasFlag(EPropertyTagFlags.HasBinaryOrNativeSerialize)
                ? EPropertyTagSerializeType.BinaryOrNative : EPropertyTagSerializeType.Property;


    public FPropertyTag(FAssetArchive Ar, PropertyInfo info, ReadType type)
    {
        Name = new FName(info.Name);
        PropertyType = new FName(info.MappingType.Type);
        ArrayIndex = info.Index;
        TagData = new FPropertyTagData(info.MappingType);
        HasPropertyGuid = false;
        PropertyGuid = null;
        PropertyTagFlags = info.ArraySize > 1 ? EPropertyTagFlags.HasArrayIndex : EPropertyTagFlags.None;

        var pos = Ar.Position;
        try
        {
            Tag = FPropertyTagType.ReadPropertyTagType(Ar, PropertyType.Text, TagData, type);
        }
        catch (ParserException e)
        {
            throw new ParserException($"Failed to read FPropertyTagType {TagData?.ToString() ?? PropertyType.Text} {Name.Text}", e);
        }

        Size = (int) (Ar.Position - pos);
    }

    public FPropertyTag(FAssetArchive Ar, bool readData)
    {
        Name = Ar.ReadFName();
        if (Name.IsNone)
            return;

        if (Ar.Ver >= EUnrealEngineObjectUE5Version.PROPERTY_TAG_COMPLETE_TYPE_NAME)
        {
            var nodes = new List<FPropertyTypeNameNode>();
            var remaining = 1;
            do
            {
                var node = new FPropertyTypeNameNode(Ar);
                nodes.Add(node);
                remaining += node.InnerCount - 1;
            }
            while (remaining > 0);

            var typeName = nodes.ToArray().AsSpan();
            PropertyType = typeName.GetName();
            TagData = new FPropertyTagData(typeName, Name.Text);

            Size = Ar.Read<int>();
            PropertyTagFlags = (EPropertyTagFlags) Ar.ReadByte();
            if (PropertyTagFlags.HasFlag(EPropertyTagFlags.BoolTrue)) TagData.Bool = true;
            ArrayIndex = PropertyTagFlags.HasFlag(EPropertyTagFlags.HasArrayIndex) ? Ar.Read<int>() : 0;
            HasPropertyGuid = PropertyTagFlags.HasFlag(EPropertyTagFlags.HasPropertyGuid);
            PropertyGuid = HasPropertyGuid ? Ar.Read<FGuid>() : null;

            if (PropertyTagFlags.HasFlag(EPropertyTagFlags.HasPropertyExtensions))
            {
                var tagExtensions = Ar.Read<EPropertyTagExtension>();
                if (tagExtensions.HasFlag(EPropertyTagExtension.OverridableInformation))
                {
                    var OverrideOperation = Ar.Read<byte>(); // EOverriddenPropertyOperation
                    var bExperimentalOverridableLogic = Ar.ReadBoolean();
                }
            }
        }
        else
        {
            PropertyType = Ar.ReadFName();

            Size = Ar.Read<int>();
            ArrayIndex = Ar.Read<int>();
            TagData = new FPropertyTagData(Ar, PropertyType.Text, Name.Text);
            if (Ar.Ver >= EUnrealEngineObjectUE4Version.PROPERTY_GUID_IN_PROPERTY_TAG)
            {
                HasPropertyGuid = Ar.ReadFlag();
                if (HasPropertyGuid)
                {
                    PropertyGuid = Ar.Read<FGuid>();
                }
            }

            if (Ar.Ver >= EUnrealEngineObjectUE5Version.PROPERTY_TAG_EXTENSION_AND_OVERRIDABLE_SERIALIZATION)
            {
                var tagExtensions = Ar.Read<EPropertyTagExtension>();
                if (tagExtensions.HasFlag(EPropertyTagExtension.OverridableInformation))
                {
                    var OverrideOperation = Ar.Read<byte>(); // EOverriddenPropertyOperation
                    var bExperimentalOverridableLogic = Ar.ReadBoolean();
                }
            }
        }

        if (!readData) return;

        var pos = Ar.Position;
        var finalPos = pos + Size;
        try
        {
            Tag = FPropertyTagType.ReadPropertyTagType(Ar, PropertyType.Text, TagData, ReadType.NORMAL);
#if DEBUG
            if (finalPos != Ar.Position)
            {
                Log.Debug("FPropertyTagType {0} {1} was not read properly, pos {2}, calculated pos {3}", TagData?.ToString() ?? PropertyType.Text, Name.Text, Ar.Position, finalPos);
            }
#endif
        }
        catch (ParserException e)
        {
#if DEBUG
            if (finalPos != Ar.Position)
            {
                Log.Warning(e, "Failed to read FPropertyTagType {0} {1}, skipping it", TagData?.ToString() ?? PropertyType.Text, Name.Text);
            }
#endif
        }
        finally
        {
            // Always seek to calculated position, no need to crash
            Ar.Position = finalPos;
        }
    }

    public override string ToString() => $"{Name.Text}  -->  {Tag?.ToString() ?? "Failed to parse"}";
}
