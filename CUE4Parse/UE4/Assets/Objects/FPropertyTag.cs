using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using System;
using System.IO;

namespace CUE4Parse.UE4.Assets.Objects
{
    public class FPropertyTag
    {
        public FName Name;
        public FName PropertyType;
        public int Size;
        public int ArrayIndex;
        public FPropertyTagData? TagData;
        public byte HasPropertyGuid;
        public FGuid? PropertyGuid;
        public FPropertyTagType? Tag;

        public FPropertyTag(FAssetArchive Ar, bool readData)
        {
            Name = Ar.ReadFName();
            if (Name.IsNone)
                return;

            PropertyType = Ar.ReadFName();
            Size = Ar.Read<int>();
            ArrayIndex = Ar.Read<int>();
            TagData = PropertyType.Text switch
            {
                "StructProperty" => new FPropertyTagData.StructProperty(Ar),
                "EnumProperty" => new FPropertyTagData.EnumOrByteProperty(Ar),
                "ByteProperty" => new FPropertyTagData.EnumOrByteProperty(Ar),
                "ArrayProperty" => new FPropertyTagData.ArrayOrSetProperty(Ar),
                "SetProperty" => new FPropertyTagData.ArrayOrSetProperty(Ar),
                "MapProperty" => new FPropertyTagData.MapProperty(Ar),
                "BoolProperty" => new FPropertyTagData.BoolProperty(Ar),
                _ => null
            };
            HasPropertyGuid = Ar.Read<byte>();
            if (HasPropertyGuid != 0)
            {
                PropertyGuid = Ar.Read<FGuid>();
            }

            if (readData)
            {
                var pos = Ar.Position;
                var finalPos = pos + Size;
                Tag = FPropertyTagType.ReadPropertyTagType(Ar, PropertyType.Text, TagData, ReadType.NORMAL);
#if DEBUG
                if (finalPos != Ar.Position)
                {
                    Console.WriteLine($"FPropertyTagType {Name.Text} ({(TagData != null ? TagData.ToString() : PropertyType.Text)}) was not read properly, pos {Ar.Position}, calculated pos {finalPos}");
                }
#endif
                Ar.Seek(finalPos, SeekOrigin.Begin);
            }
        }
    }
}