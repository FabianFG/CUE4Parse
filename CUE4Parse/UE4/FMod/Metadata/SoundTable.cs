using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Metadata;

// Sound Table is usually used for localized audio
// If present it replaces Waveform Entries completely and audio is loaded manually by the developers
// Therefore we can't associate audio in this table with any specific event automatically
public class SoundTable
{
    public readonly uint HeaderFlag;
    public readonly int SoundbankIndex;
    public readonly ulong[] Keys;
    public readonly FUInt24[] Indices;
    public readonly uint Flags;

    public SoundTable(BinaryReader Ar)
    {
        HeaderFlag = Ar.ReadUInt32();
        SoundbankIndex = Ar.ReadInt32();
        Keys = ReadSimpleArrayImp(Ar);
        Indices = FModReader.ReadSimpleArray24(Ar);
        if (FModReader.Version >= 0x7c)
            Flags = Ar.ReadUInt32();
    }

    // FMOD::SoundTable::Packed::find
    public int Find(ulong key)
    {
        int mSize = Keys.Length;
        int low = 0;
        int high = mSize - 1;

        if (high < 0)
            return -1;

        while (low <= high)
        {
            int mid = (high + low) >> 1;
            ulong currentKey = Keys[mid];

            if (key == currentKey)
                return (int) Indices[mid].Value;

            if (key < currentKey)
            {
                high = mid - 1;
            }
            else
            {
                low = mid + 1;
            }
        }

        return -1;
    }

    #region Readers
    private static ulong[] ReadSimpleArrayImp(BinaryReader Ar)
    {
        uint count = FModReader.ReadX16(Ar);

        if (count < 0) throw new InvalidDataException("Negative array length");

        var list = new ulong[count];
        for (int i = 0; i < count; i++)
        {
            list[i] = Ar.ReadUInt64();
        }

        return list;
    }
    #endregion
}
