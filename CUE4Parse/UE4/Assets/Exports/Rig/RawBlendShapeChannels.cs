using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawBlendShapeChannels
{
    public ushort[] LODs;
    public ushort[] InputIndices;
    public ushort[] OutputIndices;

    public RawBlendShapeChannels(FArchiveBigEndian Ar)
    {
        LODs = Ar.ReadArray<ushort>();
        InputIndices = Ar.ReadArray<ushort>();
        OutputIndices = Ar.ReadArray<ushort>();
    }
}
