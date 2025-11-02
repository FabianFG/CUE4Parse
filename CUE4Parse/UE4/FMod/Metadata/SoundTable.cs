using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Metadata;

public class SoundTable
{
    public readonly uint HeaderFlag;
    public readonly uint SoundbankIndex;
    public readonly long[] Keys;
    public readonly FUInt24[] Indices;
    public readonly uint Flags;

    public SoundTable(BinaryReader Ar)
    {
        HeaderFlag = Ar.ReadUInt32();
        SoundbankIndex = Ar.ReadUInt32();
        Keys = ReadSimpleArrayImp(Ar);
        Indices = FModReader.ReadSimpleArray24(Ar);
        if (FModReader.Version >= 0x7c)
            Flags = Ar.ReadUInt32();
    }

    #region Readers
    private static long[] ReadSimpleArrayImp(BinaryReader Ar)
    {
        uint count = FModReader.ReadX16(Ar);

        if (count < 0) throw new InvalidDataException("Negative array length");

        var list = new long[count];
        for (int i = 0; i < count; i++)
        {
            list[i] = Ar.ReadInt64();
        }

        return list;
    }
    #endregion
}
