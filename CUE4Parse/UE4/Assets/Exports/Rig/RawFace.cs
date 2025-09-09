using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawFace
{
    public uint[] LayoutIndices;

    public RawFace(FArchiveBigEndian Ar)
    {
        LayoutIndices = Ar.ReadArray<uint>();
    }
}
