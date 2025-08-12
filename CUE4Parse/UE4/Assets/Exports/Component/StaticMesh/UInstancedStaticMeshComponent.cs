using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;

public class UInstancedStaticMeshComponent : UStaticMeshComponent
{
    public FInstancedStaticMeshInstanceData[]? PerInstanceSMData;
    public float[]? PerInstanceSMCustomData;

    public FVector4[][]? MotoGP24Data; // PackedData

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        var bCooked = false;
        if (FFortniteMainBranchObjectVersion.Get(Ar) >= FFortniteMainBranchObjectVersion.Type.SerializeInstancedStaticMeshRenderData ||
            FEditorObjectVersion.Get(Ar) >= FEditorObjectVersion.Type.SerializeInstancedStaticMeshRenderData)
        {
            bCooked = Ar.ReadBoolean();
        }

        var bHasSkipSerializationPropertiesData = FFortniteMainBranchObjectVersion.Get(Ar) < FFortniteMainBranchObjectVersion.Type.ISMComponentEditableWhenInheritedSkipSerialization || Ar.ReadBoolean();
        if (bHasSkipSerializationPropertiesData)
        {
            switch (Ar.Game)
            {
                case EGame.GAME_Stalker2:
                    Ar.Position += 4;
                    PerInstanceSMData = Ar.ReadBulkArray(128, Ar.Read<int>(), () => new FInstancedStaticMeshInstanceData(Ar));
                    break;
                case EGame.GAME_ThroneAndLiberty:
                    var elementSize = Ar.Read<int>();
                    var elementCount = Ar.Read<int>();
                    Ar.Position -= 2 * sizeof(int);
                    switch (elementSize)
                    {
                        case 16:
                            Ar.SkipBulkArrayData();// looks like half floats, but values doesn't make sense
                            if (elementCount > 0)
                                Ar.Position += 24;
                            break;
                        case 40:
                            PerInstanceSMData = Ar.ReadArray(() => new FInstancedStaticMeshInstanceData(Ar));
                            break;
                        case 64:
                            Ar.SkipBulkArrayData();
                            break;
                        default:
                            throw new ParserException(Ar, $"Unknown element size {elementSize}");
                    }
                    break;
                default:
                    PerInstanceSMData = Ar.ReadBulkArray(() => new FInstancedStaticMeshInstanceData(Ar));
                    break;
            };

            if (FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.PerInstanceCustomData || Ar.Game == EGame.GAME_DeltaForceHawkOps)
            {
                PerInstanceSMCustomData = Ar.ReadBulkArray(Ar.Read<float>);
            }
        }

        // MOTO GP 24
        if (Ar.Game == EGame.GAME_MotoGP24)
        {
            var elemSize = Ar.Read<int>();
            var elemCount = Ar.Read<int>();

            var data = new List<FVector4[]> ();
            for (int i = 0; i < elemCount; i++) {
                var vecs = Ar.ReadArray<FVector4>(elemSize / 16); // 160 (10vecs) or 240 (15vecs)
                data.Add(vecs);
            }
            MotoGP24Data = data.ToArray();
        }

        if (bCooked && (FFortniteMainBranchObjectVersion.Get(Ar) >= FFortniteMainBranchObjectVersion.Type.SerializeInstancedStaticMeshRenderData ||
                        FEditorObjectVersion.Get(Ar) >= FEditorObjectVersion.Type.SerializeInstancedStaticMeshRenderData))
        {
            if (Ar.Game >= EGame.GAME_UE5_4)
            {
                var bHasCookedData = Ar.ReadBoolean();
                if (!bHasCookedData) return;

                Ar.SkipBulkArrayData();
                Ar.SkipBulkArrayData();
                return;
            }

            var renderDataSizeBytes = Ar.Read<ulong>();
            Ar.Position += (long) renderDataSizeBytes;
        }

        if (Ar.Game == EGame.GAME_Valorant) Ar.Position += 4;
    }

    public FInstancedStaticMeshInstanceData[] GetInstances() // PerInstanceSMData
    {
        var current = this;
        while (true) {
            if (current.PerInstanceSMData is { Length: > 0 }) {
                return current.PerInstanceSMData;
            }
            current = current.Template?.Load<UInstancedStaticMeshComponent>();
            if (current == null) {
                return [];
            }
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (PerInstanceSMData is { Length: > 0 })
        {
            writer.WritePropertyName("PerInstanceSMData");
            serializer.Serialize(writer, PerInstanceSMData);
        }

        if (PerInstanceSMCustomData is { Length: > 0 })
        {
            writer.WritePropertyName("PerInstanceSMCustomData");
            serializer.Serialize(writer, PerInstanceSMCustomData);
        }
    }
}
