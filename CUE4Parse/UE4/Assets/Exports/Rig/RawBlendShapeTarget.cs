using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawBlendShapeTarget
{
    public RawVector3Vector Deltas;
    public uint[] VertexIndices;
    public ushort BlendShapeChannelIndex;

    public RawBlendShapeTarget(FArchiveBigEndian Ar)
    {
        Deltas = new RawVector3Vector(Ar);
        VertexIndices = Ar.ReadArray<uint>();
        BlendShapeChannelIndex = Ar.Read<ushort>();
    }
}
