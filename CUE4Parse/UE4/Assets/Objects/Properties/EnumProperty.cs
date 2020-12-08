using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Assets.Objects
{
    public class EnumProperty : FPropertyTagType<FName>
    {
        public EnumProperty(FAssetArchive Ar, FPropertyTagData? tagData, ReadType type)
        {
            if (type == ReadType.ZERO)
            {
                Value = new FName(IndexToEnum(Ar, tagData?.EnumName, 0));
            }
            else if (Ar.HasUnversionedProperties && type == ReadType.NORMAL)
            {
                var index = 0;
                if (tagData?.InnerType != null)
                {
                    var underlyingProp = ReadPropertyTagType(Ar, tagData.InnerType, tagData.InnerTypeData, ReadType.NORMAL)?.GenericValue;
                    if (underlyingProp != null && underlyingProp.IsNumericType())
                        index = Convert.ToInt32(underlyingProp);
                }
                else
                {
                    index = Ar.Read<byte>();
                }
                Value = new FName(IndexToEnum(Ar, tagData?.EnumName, index));
            }
            else
            {
                Value = tagData?.EnumName == null ? new FName() : Ar.ReadFName();
            }
        }

        private static string IndexToEnum(FAssetArchive Ar, string? enumName, int index)
        {
            if (enumName == null)
                return index.ToString();

            if (Ar.Owner.Mappings != null &&
                Ar.Owner.Mappings.Enums.TryGetValue(enumName, out var values) &&
                values.TryGetValue(index, out var member))
            {
                return string.Concat(enumName, "::", member);
            }
            else
            {
                return string.Concat(enumName, "::", index);
            }
        }
    }
}