using System.Collections.Generic;
using CUE4Parse_Conversion.UEFormat;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.PhysicsEngine;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Meshes.UEFormat.Collision;

public readonly struct FConvexMeshCollision(FKConvexElem ConvexElem) : ISerializable
{
    public void Serialize(FArchiveWriter Ar)
    {
        Ar.WriteFString(ConvexElem.Name.Text);
        Ar.WriteArray(ConvexElem.VertexData, (writer, vector) =>
        {
            vector.Y = -vector.Y;
            vector.Serialize(writer);
        });
        Ar.WriteArray(ConvexElem.IndexData, (writer, index) => writer.Write(index));
    }
}