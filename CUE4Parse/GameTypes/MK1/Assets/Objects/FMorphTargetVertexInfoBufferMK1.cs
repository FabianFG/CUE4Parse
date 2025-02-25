using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.GameTypes.MK1.Assets.Objects;

public struct FMorphTargetDeltaMK1
{
    public FHalfVector Postion;
    public FHalfVector Tangent;
}

public class FMorphTargetVertexInfoBufferMK1
{
    public FMorphTargetDelta[] Vertices;
    public uint[] Offsets;
    public int[] Sizes;

    public FMorphTargetVertexInfoBufferMK1(FAssetArchive Ar)
    {
        var Indices = Ar.ReadBulkArray<uint>();
        var Deltas = Ar.ReadBulkArray<FMorphTargetDeltaMK1>();
        Vertices = new FMorphTargetDelta[Deltas.Length];
        for (var i = 0; i < Deltas.Length; i++)
        {
            Vertices[i] = new FMorphTargetDelta(Deltas[i].Postion, Deltas[i].Tangent, Indices[i]);
        }
        // FVector4[] max and min
        Ar.SkipBulkArrayData();
        Ar.SkipBulkArrayData();
        Offsets = Ar.ReadBulkArray<uint>();
        Sizes = Ar.ReadBulkArray<int>();
        Ar.SkipBulkArrayData(); // bools
        Ar.Position += 8;
    }
}
