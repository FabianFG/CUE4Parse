using System;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties;

[JsonConverter(typeof(BytePropertyConverter))]
public class ByteProperty : FPropertyTagType<byte>
{
    public ByteProperty(FArchive Ar, ReadType type)
    {
        Value = type switch
        {
            ReadType.ZERO => 0,
            ReadType.NORMAL => Ar.Read<byte>(),
            ReadType.MAP when Ar.Versions["ByteProperty.TMap64Bit"] => (byte) Ar.Read<ulong>(),
            ReadType.MAP when Ar.Versions["ByteProperty.TMap16Bit"] => (byte) Ar.Read<ushort>(),
            ReadType.MAP when Ar.Versions["ByteProperty.TMap8Bit"] => Ar.Read<byte>(),
            ReadType.MAP => Ar.Read<byte>(),
            ReadType.ARRAY => Ar.Read<byte>(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
