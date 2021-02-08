using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.MappingsProvider;
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
            ArrayIndex = 0;
            TagData = new FPropertyTagData(info.MappingType);
            HasPropertyGuid = false;
            PropertyGuid = null;

            var pos = Ar.Position;
            try
            {
                Tag = FPropertyTagType.ReadPropertyTagType(Ar, PropertyType.Text, TagData,
                    type);
            }
            catch (ParserException e)
            {
                Log.Debug(
                    $"Failed to read FPropertyTagType {Name.Text} ({(TagData != null ? TagData.ToString() : PropertyType.Text)}), skipping it");
                Log.Debug(e.ToString());
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
            TagData = new FPropertyTagData(Ar, PropertyType.Text);
            HasPropertyGuid = Ar.ReadFlag();
            if (HasPropertyGuid)
            {
                PropertyGuid = Ar.Read<FGuid>();
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
                        Log.Debug(
                            $"FPropertyTagType {Name.Text} ({(TagData != null ? TagData.ToString() : PropertyType.Text)}) was not read properly, pos {Ar.Position}, calculated pos {finalPos}");
                    }
#endif
                }
                catch (ParserException e)
                {
#if DEBUG
                    if (finalPos != Ar.Position)
                    {
                        Log.Debug(
                            $"Failed to read FPropertyTagType {Name.Text} ({(TagData != null ? TagData.ToString() : PropertyType.Text)}), skipping it");
                        Log.Debug(e.ToString());
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