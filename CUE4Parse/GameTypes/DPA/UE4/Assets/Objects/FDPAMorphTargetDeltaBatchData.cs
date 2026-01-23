using System.Linq;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.GameTypes.DPA.UE4.Assets.Objects;

public class FDPAMorphTargetDeltaBatchData(FArchive Ar)
{
    public byte[] Indices = Ar.ReadArray<byte>();
    public TPair<FHalfVectorScaled>[] VertData = Ar.Game is EGame.GAME_DarkPicturesAnthologyLittleHope
            ? Ar.ReadArray(() => new TPair<FHalfVectorScaled>(Ar.Read<FHalfVector>(), Ar.Read<FHalfVector>()))
            : Ar.ReadArray(Ar.Read<TPair<FHalfVectorScaled>>);
    public uint StartIndex = Ar.Read<uint>();

    public static FMorphTargetDelta[] ProcessDPAMorphTargetDeltas(FArchive Ar)
    {
        var batches = Ar.ReadArray(() => new FDPAMorphTargetDeltaBatchData(Ar));
        var size = batches.Sum(b => b.VertData.Length);
        var deltas = new FMorphTargetDelta[size];
        var k = 0;
        foreach (var batch in batches)
        {
            for (int i = 0; i < batch.VertData.Length; i++)
            {
                var delta = batch.VertData[i];
                deltas[k++] = new FMorphTargetDelta(delta.X, delta.Y, batch.StartIndex + batch.Indices[i]);
            }
        }
        return deltas;
    }
}
