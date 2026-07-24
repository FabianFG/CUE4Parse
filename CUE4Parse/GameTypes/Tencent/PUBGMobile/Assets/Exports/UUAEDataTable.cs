using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Objects.Unversioned;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.Tencent.PUBGMobile.Assets.Exports;

public class UUAEDataTable : UDataTable
{
    private const uint RAW_ROWS_PUBG_MAGIC = 0x20201028;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        var baseObject = new PUBGMobileDataTableBase
        {
            Name = Name,
            Class = Class,
            Outer = Outer,
            Super = Super,
            Template = Template,
            Flags = Flags
        };
        baseObject.Deserialize(Ar, validPos);
        Properties = baseObject.SerializedProperties;

        var rowStructIndex = GetOrDefault<FPackageIndex?>("RowStruct");
        if (rowStructIndex?.Load<UStruct>() is not { } rowStruct)
        {
            Log.Warning("Can't find or load RowStruct type to serialize UAEDataTable");
            return;
        }

        RowStructName = rowStruct.Name;
        var startPosition = Ar.Position;
        if (Ar.Read<uint>() != RAW_ROWS_PUBG_MAGIC)
        {
            Ar.Position = startPosition;
            var taggedNumRows = Ar.Read<int>();
            if (taggedNumRows < 0)
                throw new ParserException(Ar, $"Invalid UAEDataTable row count {taggedNumRows}");

            RowMap = new Dictionary<FName, FStructFallback>(taggedNumRows);
            for (var i = 0; i < taggedNumRows; i++)
                RowMap[Ar.ReadFName()] = new FStructFallback(Ar, rowStruct);
            return;
        }

        var properties = (rowStruct.Children ?? [])
            .Select(child => child.Load())
            .OfType<UProperty>()
            .Select(property => (Property: property, Type: CreatePropertyType(property)))
            .ToArray();
        var numRows = Ar.Read<int>();
        if (numRows < 0)
            throw new ParserException(Ar, $"Invalid UAEDataTable row count {numRows}");

        RowMap = new Dictionary<FName, FStructFallback>(numRows);
        for (var rowIndex = 0; rowIndex < numRows; rowIndex++)
        {
            var rowName = Ar.ReadFName();
            if (properties.Length == 0)
            {
                RowMap[rowName] = new FStructFallback(Ar, RowStructName, FRawHeader.FullRead, ReadType.RAW);
                continue;
            }

            var rowProperties = new List<FPropertyTag>(properties.Length);
            foreach (var (property, type) in properties)
            {
                for (var i = 0; i < property.ArrayDim; i++)
                {
                    var info = new PropertyInfo(i, property.Name, type, property.ArrayDim);
                    var readType = property switch
                    {
                        UBoolProperty => ReadType.ARRAY,
                        USetProperty or UMapProperty => ReadType.NORMAL,
                        _ => ReadType.RAW
                    };
                    var tag = new FPropertyTag(Ar, info, readType);
                    if (tag.Tag == null)
                        throw new ParserException(Ar, $"Unsupported UAEDataTable property {property.GetType().Name} {property.Name}");
                    rowProperties.Add(tag);
                }
            }
            RowMap[rowName] = new FStructFallback(rowProperties);
        }
    }

    private static PropertyType CreatePropertyType(UProperty property)
    {
        var type = property.GetType().Name[1..];
        var result = new PropertyType(type);
        switch (property)
        {
            case UArrayProperty array when array.Inner.Load<UProperty>() is { } inner:
                result.InnerType = CreatePropertyType(inner);
                break;
            case UByteProperty byteProperty when byteProperty.Enum.TryLoad<UEnum>(out var byteEnum):
                result.Enum = byteEnum;
                result.EnumName = byteEnum.Name;
                break;
            case UEnumProperty enumProperty:
                result.Enum = enumProperty.Enum.Load<UEnum>();
                result.EnumName = enumProperty.Enum.Name;
                if (enumProperty.UnderlyingProp.Load<UProperty>() is { } underlying)
                    result.InnerType = CreatePropertyType(underlying);
                break;
            case UMapProperty map:
                if (map.KeyProp.Load<UProperty>() is { } key)
                    result.InnerType = CreatePropertyType(key);
                if (map.ValueProp.Load<UProperty>() is { } value)
                    result.ValueType = CreatePropertyType(value);
                break;
            case USetProperty set when set.ElementProp.Load<UProperty>() is { } element:
                result.InnerType = CreatePropertyType(element);
                break;
            case UStructProperty structProperty:
                result.Struct = structProperty.Struct.Load<UStruct>();
                result.StructType = structProperty.Struct.Name;
                break;
        }
        return result;
    }

    private sealed class PUBGMobileDataTableBase : UObject
    {
        public List<FPropertyTag> SerializedProperties => Properties;
    }
}
