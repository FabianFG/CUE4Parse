using System;
using CUE4Parse.GameTypes.OuterWorlds2.Objects;
using CUE4Parse.GameTypes.OuterWorlds2.Readers;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using Serilog;

namespace CUE4Parse.GameTypes.OuterWorlds2.Properties;

public class FOW2ObjectProperty : ObjectProperty
{
    public FOW2ObjectProperty(FOW2ObjectsArchive Ar, ReadType type) : base(Ar, ReadType.ZERO)
    {
        var data = Ar.Read<uint>();
        var datatype = (data >> 24) & 0xFF; // 0x93
        var index = (int) (data & 0xFFFFFF);
        Value = type == ReadType.ZERO ? new FPackageIndex(Ar.Asset, 0) : Ar.Objects.ObjectStore[index];
    }
}

public class FOW2FPropertyTag : FPropertyTag
{
    public FPropertryDataObjectContainer Objects;
    public bool bHasVersion;
    public bool bIsDefault;
    public FOW2FPropertyTag(FAssetArchive Ar)
    {
        Name = Ar.ReadFName();
        if (Name.IsNone)
            return;

        bHasVersion = Ar.ReadBoolean();
        Span<FPropertyTypeNameNode> typeName = [new FPropertyTypeNameNode() { Name = Ar.ReadFName() }];
        bIsDefault = Ar.ReadBoolean();
        PropertyType = typeName.GetName();
        TagData = new FPropertyTagData(typeName, Name.Text);

        var data = Ar.ReadArray<byte>();
        Size = data.Length;
        Objects = new FPropertryDataObjectContainer(Ar);
        using var byteAr = new FByteArchive("FOW2ObjectsArchive", data, Ar.Versions);
        using var objectAr = new FOW2ObjectsArchive(byteAr, Ar.Owner, Objects, bHasVersion);

        var idk = objectAr.Read<int>();
        if (PropertyType.Text == "ArrayProperty")
        {
            objectAr.SkipFString();
            var len = objectAr.Read<int>();
            if (Name.Text == "WaveIndexes")
            {
                TagData.InnerType = "IntProperty";
            }
            else if (Name.Text == "CoverObject")
            {
                TagData.InnerType = "SoftObjectProperty";
            }
        }
        else if (PropertyType.Text == "BoolProperty")
        {
            TagData.Bool = objectAr.ReadFlag();
        }

        try
        {
            Tag = FPropertyTagType.ReadPropertyTagType(objectAr, PropertyType.Text, TagData, ReadType.NORMAL, Size);
#if DEBUG
            if (objectAr.Position != objectAr.Length)
            {
                Log.Debug("FPropertyTagType {0} {1} was not read properly, pos {2}, calculated pos {3}", TagData?.ToString() ?? PropertyType.Text, Name.Text, objectAr.Position, objectAr.Length);
            }
#endif
        }
        catch (ParserException e)
        {
#if DEBUG
            if (objectAr.Position != objectAr.Length)
            {
                Log.Warning(e, "Failed to read FPropertyTagType {0} {1}, skipping it", TagData?.ToString() ?? PropertyType.Text, Name.Text);
            }
#endif
        }
    }
}
