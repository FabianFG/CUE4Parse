using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Animation;

public class UMorphTarget : UObject
{
    public FMorphTargetLODModel[] MorphLODModels = [new()];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
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

        var stripFlags = Ar.Read<FStripDataFlags>();
        if (stripFlags.IsAudioVisualDataStripped())
            return;

        var bCooked = FFortniteMainBranchObjectVersion.Get(Ar) >= FFortniteMainBranchObjectVersion.Type.MorphTargetCookedCPUDataCompressed && Ar.ReadBoolean();
        MorphLODModels = Ar.ReadArray(() => new FMorphTargetLODModel(Ar));

        if (bCooked)
        {
            var bStoreCompressedVertices = Ar.ReadBoolean();

            if (bStoreCompressedVertices)
            {
                var numLODs = Ar.Read<int>();

                for (int lodIndex = 0; lodIndex < numLODs; lodIndex++)
                {
                    var packedDeltaHeaders = Ar.ReadArray(() => new FDeltaBatchHeader(Ar));
                    var packedDeltaData = Ar.ReadArray<uint>();
                    var positionPrecision = Ar.Read<float>();
                    var tangentPrecision = Ar.Read<float>();

                    if (MorphLODModels.Length <= 0)
                        throw new NotSupportedException("CPU Based MorphModels decoding is currently not supported");
                }
            }
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(MorphLODModels));
        serializer.Serialize(writer, MorphLODModels);
    }
}
