using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Serilog;

namespace CUE4Parse.GameTypes.OctopathTraveler.Exports;

public class UBinaryAsset : UObject
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Game != EGame.GAME_OctopathTraveler0) return;

        try
        {
            var data = GetOrDefault<byte[]>("BinaryData", []);
            using var dataAr = new FByteArchive("BinaryData", data, Ar.Versions);
            var name = Name;

            if (name.StartsWith("VillageBuildingFillMap"))
            {
                var str = new FPropertyTag
                {
                    Name = "Data",
                    PropertyType = "StrProperty",
                    Tag = new StrProperty(Encoding.UTF8.GetString(data)),
                    TagData = new FPropertyTagData(new PropertyType("StrProperty")),
                };
                Properties.Clear();
                Properties.Add(str);
                return;
            }

            var tag = new FPropertyTag
            {
                Name = "Data",
                Tag = ReadOctopathPropertyTagType(dataAr),
            };
            if (dataAr.Position != dataAr.Length)
                Log.Warning("Did not read the full UBinaryAsset data for {0}, read {1} of {2} bytes", Name, dataAr.Position, dataAr.Length);

            Properties.Clear();
            Properties.Add(tag);
        }
        catch (Exception e)
        {
            Log.Error(e,"Failed to parse OctopathTraveler0 UBinaryAsset {0}", Name);
        }
    }

    public static FPropertyTagType? ReadOctopathPropertyTagType(FArchive Ar)
    {
        var type = Ar.Read<byte>();
        Ar.Position -= 1;
        return type switch
        {
            0xc2 or 0xc3 => new BoolProperty((Ar.Read<byte>() & 1) == 1),
            >= 0x00 and <= 0x7f or 0xd2 or >= 0xe0 and <= 0xff => new IntProperty(ReadIntValue(Ar)),
            >= 0xca and <= 0xd3 => ReadNumericProperty(Ar),
            >= 0xa0 and <= 0xbf or >= 0xd9 and <= 0xdb => new StrProperty(ReadString(Ar)),
            >= 0x80 and <= 0x8f or 0xde or 0xdf => new StructProperty(ReadStruct(Ar)),
            >= 0x90 and <= 0x9f or >= 0xc4 and <= 0xc9 or >= 0xd4 and <= 0xd8 or 0xdc or 0xdd => new ArrayProperty(ReadArray(Ar)),

            0xc0 => null,
            0xc1 => throw new ParserException(Ar, "Unknown property type 0xc1"),
        };

        FPropertyTagType? ReadNumericProperty(FArchive Ar)
        {
            var numType = Ar.Read<byte>();
            return numType switch
            {
                0xca => new FloatProperty(BinaryPrimitives.ReadSingleBigEndian(Ar.ReadBytes(4))),
                0xcb => new DoubleProperty(BinaryPrimitives.ReadDoubleBigEndian(Ar.ReadBytes(8))),
                0xcc => new ByteProperty(Ar.Read<byte>()),
                0xcd => new UInt16Property(BinaryPrimitives.ReverseEndianness(Ar.Read<ushort>())),
                0xce => new UInt32Property(BinaryPrimitives.ReverseEndianness(Ar.Read<uint>())),
                0xcf => new UInt64Property(BinaryPrimitives.ReverseEndianness(Ar.Read<ulong>())),
                0xd0 => new Int8Property(Ar.Read<sbyte>()),
                0xd1 => new Int16Property(BinaryPrimitives.ReverseEndianness(Ar.Read<short>())),
                0xd3 => new Int64Property(BinaryPrimitives.ReverseEndianness(Ar.Read<long>())),
                _ => throw new ParserException(Ar, $"Unknown NumericProperty type: {numType:X2}"),
            };
        }

        string ReadString(FArchive Ar)
        {
            var type = Ar.Read<byte>();
            int length = type switch
            {
                >= 0xa0 and <= 0xbf => type & 0x1f,
                0xd9 => Ar.Read<byte>(),
                0xda => BinaryPrimitives.ReverseEndianness(Ar.Read<ushort>()),
                0xdb => (int)BinaryPrimitives.ReverseEndianness(Ar.Read<uint>()),
                _ => throw new ParserException(Ar, $"Unknown string property type: {type:X2}"),
            };

            return Encoding.UTF8.GetString(Ar.ReadBytes(length));
        }

        int ReadIntValue(FArchive Ar)
        {
            var type = Ar.Read<byte>();
            return type switch
            {
                >= 0x00 and <= 0x7f => type & 0x7f,
                >= 0xe0 and <= 0xff => (type & 0x1f),
                0xd2 => BinaryPrimitives.ReverseEndianness(Ar.Read<int>()),
                _ => throw new ParserException(Ar, $"Unknown int property type: {type:X2}"),
            };
        }

        UScriptArray ReadArray(FArchive Ar)
        {
            var pos = Ar.Position;
            var type = Ar.Read<byte>();

            var length = type switch
            {
                >= 0xc4 and <= 0xc or >= 0xd4 and <= 0xd8 => throw new NotImplementedException("OctopathTraveler array with type info inside is not implemented"),
                >= 0x90 and <= 0x9f => type & 0xf,
                0xdc => BinaryPrimitives.ReverseEndianness(Ar.Read<ushort>()),
                0xdd => (int) BinaryPrimitives.ReverseEndianness(Ar.Read<uint>()),

                _ => throw new ParserException(Ar, $"Unknown ArrayProperty type: {type:X2}"),
            };

            var properties = new List<FPropertyTagType>(length);
            for (int i = 0; i < length; i++)
            {
                properties.Add(ReadOctopathPropertyTagType(Ar));
            }

            return new UScriptArray(properties, "");
        }

        FScriptStruct ReadStruct(FArchive Ar)
        {
            var type = Ar.Read<byte>();

            var length = type switch
            {
                >= 0x80 and <= 0x8f => type & 0xf,
                0xde => BinaryPrimitives.ReverseEndianness(Ar.Read<ushort>()),
                0xdf => (int) BinaryPrimitives.ReverseEndianness(Ar.Read<uint>()),
                _ => throw new ParserException(Ar, $"Unknown StructProperty type: {type:X2}"),
            };
            var properties = new List<FPropertyTag>();
            for (int i = 0; i < length; i++)
            {
                var name = ReadString(Ar);

                var tag = new FPropertyTag { Name = new FName(name) };

                var pos = Ar.Position;
                try
                {
                    tag.Tag = ReadOctopathPropertyTagType(Ar);
                }
                catch (ParserException e)
                {
                    throw new ParserException($"Failed to read FPropertyTagType {tag.TagData?.ToString() ?? tag.PropertyType.Text} {tag.Name.Text}", e);
                }

                tag.Size = (int) (Ar.Position - pos);

                if (tag.Tag != null)
                    properties.Add(tag);
                else
                    throw new ParserException(Ar, $"Failed to serialize property {tag.Name}. Can't proceed with serialization (Serialized {properties.Count} properties until now)");

            }
            var res = new FStructFallback();
            res.Properties.AddRange(properties);
            return new FScriptStruct(res);
        }
    }
}
