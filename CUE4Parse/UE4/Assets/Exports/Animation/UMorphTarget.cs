using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports.NavigationSystem.Detour;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Animation;

public class UMorphTarget : UObject
{
    public FMorphTargetLODModel[] MorphLODModels = [new()];
    private FMorphTargetCompressedLODModel[] _compressedLODModels = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        if (Ar.Game == EGame.GAME_WorldofJadeDynasty) Ar.Position += 4;
        base.Deserialize(Ar, validPos);

        if (!Ar.Versions["MorphTarget"])
        {
            Ar.Position = validPos;
            return;
        }

        if (Ar.Game == EGame.GAME_MortalKombat1)
        {
            Ar.Position += 38;
            return;
        }

        var stripFlags = new FStripDataFlags(Ar);
        if (stripFlags.IsAudioVisualDataStripped())
            return;

        var bCooked = FFortniteMainBranchObjectVersion.Get(Ar) >= FFortniteMainBranchObjectVersion.Type.MorphTargetCookedCPUDataCompressed && Ar.ReadBoolean();
        if (Ar.Game is EGame.GAME_NevernessToEverness) bCooked = Ar.ReadBoolean();

        MorphLODModels = Ar.ReadArray(() => new FMorphTargetLODModel(Ar));

        if (Ar.Game is EGame.GAME_RocoKingdomWorld)
        {
            var posscales = GetOrDefault<float[]>("MorphPosDeltaCompressExtent", []);
            var tanscales = GetOrDefault<float[]>("MorphTanDeltaCompressExtent", []);
            var scales = posscales.Zip(tanscales).ToArray();

            if (scales.Length > 0)
            {
                for (int i = 0; i < scales.Length; i++)
                {
                    var (posscale, tanscale) = scales[i];
                    for (int j = 0; j < MorphLODModels[i].Vertices.Length; j++)
                    {
                        var delta = MorphLODModels[i].Vertices[j];
                        delta.PositionDelta *= posscale;
                        delta.TangentZDelta *= tanscale;
                    }
                }
            }
        }

        if (bCooked)
        {
            var bStoreCompressedVertices = Ar.ReadBoolean();

            if (bStoreCompressedVertices)
            {
                var numLODs = Ar.Read<int>();
                _compressedLODModels = new FMorphTargetCompressedLODModel[numLODs];

                for (int lodIndex = 0; lodIndex < numLODs; lodIndex++)
                {
                    var packedDeltaHeaders = Ar.ReadArray(() => new FDeltaBatchHeader(Ar));
                    var packedDeltaData = Ar.ReadArray<uint>();
                    var positionPrecision = Ar.Read<float>();
                    var tangentPrecision = Ar.Read<float>();

                    _compressedLODModels[lodIndex] = new FMorphTargetCompressedLODModel(packedDeltaHeaders, packedDeltaData, positionPrecision, tangentPrecision);
                    if (MorphLODModels.Length <= 0)
                        throw new NotSupportedException("CPU Based MorphModels decoding is currently not supported");
                }
            }
        }
    }

    public bool TryGetCompressedLODModel(int index, [MaybeNullWhen(false)] out FMorphTargetCompressedLODModel compressedLodModel)
    {
        if (index >= 0 && index < _compressedLODModels.Length)
        {
            compressedLodModel = _compressedLODModels[index];
            return true;
        }

        compressedLodModel = null;
        return false;
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(MorphLODModels));
        serializer.Serialize(writer, MorphLODModels);
    }
}
