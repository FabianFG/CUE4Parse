using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.V2.Writers.ActorX;

public readonly struct VMorphInfo(string morphName, int vertexCount)
{
    public readonly string MorphName = morphName;
    public readonly int VertexCount = vertexCount;

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Write(MorphName, 64);
        Ar.Write(VertexCount);
    }
}
