using System;
using CUE4Parse_Conversion.ActorX;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Meshes.PSK;

public class VMorphInfo
{
    public readonly string MorphName;
    public readonly int VertexCount;

    public VMorphInfo(string morphName, int vertexCount)
    {
        MorphName = morphName;
        VertexCount = vertexCount;
    }
    
    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Write(MorphName.Substring(0, Math.Min(MorphName.Length, 64)), 64);
        Ar.Write(VertexCount);
    }
}