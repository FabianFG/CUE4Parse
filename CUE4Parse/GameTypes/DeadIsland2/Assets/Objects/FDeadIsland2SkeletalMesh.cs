using System.Numerics.Tensors;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.GameTypes.DeadIsland2.Assets.Objects;

public static class FDeadIsland2SkeletalMesh
{
    public static void SkipExtraVertexData(FArchive Ar)
    {
        Ar.Position += 8; // Unknown and vertex stride
        var numVertices = Ar.Read<int>();
        if (numVertices > 0)
            Ar.SkipBulkArrayData();
        _ = Ar.ReadBoolean();
    }

    public static void ConvertIndices(FMultisizeIndexContainer buffer, FSkelMeshSection[] sections)
    {
        if (buffer.Buffer is not { } indices)
            return;

        foreach (var section in sections)
        {
            var sectionIndices = indices.AsSpan(section.BaseIndex, section.NumTriangles * 3);
            if (sectionIndices.IsEmpty)
                continue;

            var min = TensorPrimitives.Min(sectionIndices);
            var max = TensorPrimitives.Max(sectionIndices);
            if (min >= section.BaseVertexIndex && max < section.BaseVertexIndex + section.NumVertices)
                continue;

            if (max >= section.NumVertices)
                throw new ParserException("Invalid Dead Island 2 skeletal mesh indices");

            TensorPrimitives.Add(sectionIndices, section.BaseVertexIndex, sectionIndices);
        }
    }
}
