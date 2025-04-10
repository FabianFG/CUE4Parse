using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawVertexLayoutVector
{
    public uint[] Positions;
    public uint[] TextureCoordinates;
    public uint[] Normals;

    public RawVertexLayoutVector(FArchiveBigEndian Ar)
    {
        Positions = Ar.ReadArray<uint>();
        TextureCoordinates = Ar.ReadArray<uint>();
        Normals = Ar.ReadArray<uint>();
    }
}
