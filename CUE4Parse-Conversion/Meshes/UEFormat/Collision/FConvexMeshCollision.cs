using System.Collections.Generic;
using CUE4Parse_Conversion.UEFormat;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.PhysicsEngine;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Meshes.UEFormat.Collision;

public readonly struct FConvexMeshCollision : ISerializable
{
    private readonly string Name;
    private readonly List<FVector> Vertices = [];
    private readonly int[] Indices;

    public FConvexMeshCollision(FKConvexElem convexElem)
    {
        Name = convexElem.Name.Text;
        Indices = convexElem.IndexData;
        
        foreach (var vertex in convexElem.VertexData)
        {
            var serializeVertex = vertex;
            serializeVertex.Y = -serializeVertex.Y;
            Vertices.Add(serializeVertex);
        }
    }
    
    public void Serialize(FArchiveWriter Ar)
    {
        Ar.WriteFString(Name);
        Ar.WriteArray(Vertices, (writer, vector) => vector.Serialize(writer));
        Ar.WriteArray(Indices, (writer, index) => writer.Write(index));
    }
}