using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawMesh
{
    public uint Offset;
    public RawVector3Vector Positions;
    public RawTextureCoordinateVector TextureCoordinates;
    public RawVector3Vector Normals;
    public RawVertexLayoutVector Layouts;
    public RawFace[] Faces;
    public ushort MaximumInfluencePerVertex;
    public RawVertexSkinWeights[] SkinWeights;
    public RawBlendShapeTarget[] BlendShapeTargets;

    public RawMesh(FArchiveBigEndian Ar)
    {
        Offset = Ar.Read<uint>();
        Positions = new RawVector3Vector(Ar);
        TextureCoordinates = new RawTextureCoordinateVector(Ar);
        Normals = new RawVector3Vector(Ar);
        Layouts = new RawVertexLayoutVector(Ar);
        Faces = Ar.ReadArray(() => new RawFace(Ar));
        MaximumInfluencePerVertex = Ar.Read<ushort>();
        SkinWeights = Ar.ReadArray(() => new RawVertexSkinWeights(Ar));
        BlendShapeTargets = Ar.ReadArray(() => new RawBlendShapeTarget(Ar));
    }
}
