using System.Diagnostics;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Component.StaticMesh
{
    public class UInstancedStaticMeshComponent : UStaticMeshComponent
    {
        public FInstancedStaticMeshInstanceData[]? PerInstanceSMData;
        public float[]? PerInstanceSMCustomData;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            if (Ar.Owner?.Provider?.InternalGameName.ToUpper() == "FORTNITEGAME")
            {
                var read = Ar.Read<uint>();
                switch (read)
                {
                    case 1:
                        Ar.Position += 60;
                        break;
                    case 53009:
                        Ar.Position += 52;
                        break;
                    default:
                        Debugger.Break();
                        break;
                }
            }

            var bCooked = false;
            if (FFortniteMainBranchObjectVersion.Get(Ar) >= FFortniteMainBranchObjectVersion.Type.SerializeInstancedStaticMeshRenderData ||
                FEditorObjectVersion.Get(Ar) >= FEditorObjectVersion.Type.SerializeInstancedStaticMeshRenderData)
            {
                bCooked = Ar.ReadBoolean();
            }

            var bHasSkipSerializationPropertiesData = FFortniteMainBranchObjectVersion.Get(Ar) < FFortniteMainBranchObjectVersion.Type.ISMComponentEditableWhenInheritedSkipSerialization || Ar.ReadBoolean();
            if (bHasSkipSerializationPropertiesData)
            {
                PerInstanceSMData = Ar.ReadBulkArray(() => new FInstancedStaticMeshInstanceData(Ar));
                if (FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.PerInstanceCustomData)
                {
                    PerInstanceSMCustomData = Ar.ReadBulkArray(Ar.Read<float>);
                }
            }

            if (bCooked && (FFortniteMainBranchObjectVersion.Get(Ar) >= FFortniteMainBranchObjectVersion.Type.SerializeInstancedStaticMeshRenderData ||
                            FEditorObjectVersion.Get(Ar) >= FEditorObjectVersion.Type.SerializeInstancedStaticMeshRenderData))
            {
                var renderDataSizeBytes = Ar.Read<ulong>();
                if (renderDataSizeBytes > 0)
                {
                    // FStaticMeshInstanceData::Serialize
                    Ar.Position = validPos;
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
}
