using System;
using System.Collections.Generic;
using CUE4Parse.GameTypes.FF7.Assets.Exports;
using CUE4Parse.GameTypes.FF7.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;

namespace CUE4Parse.GameTypes.FF7.Objects;

public class FMemoryMappedImageArchive(FArchive Ar) : FMemoryImageArchive(Ar)
{
    public override FName ReadFName()
    {
        if (Names != null && Names.TryGetValue((int) Position, out (FName name, bool bIsScriptName) name))
        {
            Position += name.bIsScriptName ? 12 : 8;
            return name.name;
        }
        Position += 8;
        return default;
    }

    public FF7StructProperty DeserializeProperties(FF7Property[] structProperties)
    {
        var properties = new List<FPropertyTagType>();
        foreach (var prop in structProperties)
        {
            if (prop.Name.Text.EndsWith("_Array"))
            {
                var tag = new FF7ArrayProperty(this, prop.UnderlyingType);
                properties.Add(tag);
            }
            else
            {
                var tag = ReadPropertyTagType(prop.UnderlyingType);
                properties.Add(tag);
            }
        }

        return new FF7StructProperty(properties, structProperties);
    }

    public FPropertyTagType? ReadPropertyTagType(FF7propertyType underlyingType)
    {
        return underlyingType switch
        {
            FF7propertyType.BoolProperty => new FF7BoolProperty(this),
            FF7propertyType.ByteProperty => new FF7ByteProperty(this),
            FF7propertyType.Int8Property => new FF7Int8Property(this),
            FF7propertyType.UInt16Property => new FF7UInt16Property(this),
            FF7propertyType.Int16Property => new FF7Int16Property(this),
            FF7propertyType.UIntProperty => new FF7UIntProperty(this),
            FF7propertyType.IntProperty => new FF7IntProperty(this),
            FF7propertyType.Int64Property => new FF7Int64Property(this),
            FF7propertyType.FloatProperty => new FF7FloatProperty(this),
            FF7propertyType.StrProperty => new FF7StrProperty(this),
            FF7propertyType.NameProperty => new FF7NameProperty(this),
            _ => throw new ParserException(this, $"Unknown property type {underlyingType}")
        };
    }

    public override T[] ReadArray<T>(Func<T> getter)
    {
        var initialPos = Position;
        var dataPtr = new FFrozenMemoryImagePtr(this);
        var arrayNum = Read<int>();
        var arrayMax = Read<int>();
        if (arrayNum != arrayMax)
        {
            throw new ParserException(this, $"Num ({arrayNum}) != Max ({arrayMax})");
        }
        if (arrayNum == 0)
        {
            return [];
        }

        var continuePos = Position;
        Position = initialPos + dataPtr.OffsetFromThis;
        var data = new T[arrayNum];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = getter();
            //Position = Position.Align(ArrayAlign);
        }
        Position = continuePos;
        return data;
    }

    public T[] ReadArray<T>(Func<T> getter, int align)
    {
        var initialPos = Position;
        var dataPtr = new FFrozenMemoryImagePtr(this);
        var arrayNum = Read<int>();
        var arrayMax = Read<int>();
        if (arrayNum != arrayMax)
        {
            throw new ParserException(this, $"Num ({arrayNum}) != Max ({arrayMax})");
        }
        if (arrayNum == 0)
        {
            return [];
        }

        var continuePos = Position;
        Position = initialPos + dataPtr.OffsetFromThis;
        var data = new T[arrayNum];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = getter();
            Position = Position.Align(align);
        }
        Position = continuePos;
        return data;
    }
}
