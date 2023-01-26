using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Serilog;

namespace CUE4Parse.UE4.Assets.Objects
{
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

        public FPropertyTag(FAssetArchive Ar, PropertyInfo info, ReadType type)
        {
            Name = new FName(info.Name);
            PropertyType = new FName(info.MappingType.Type);
            ArrayIndex = info.Index;
            TagData = new FPropertyTagData(info.MappingType);
            HasPropertyGuid = false;
            PropertyGuid = null;

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

            if (readData)
            {
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
        }

        public override string ToString() => $"{Name.Text}  -->  {Tag?.ToString() ?? "Failed to parse"}";
    }
}
