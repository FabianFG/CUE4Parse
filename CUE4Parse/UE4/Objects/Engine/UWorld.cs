using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Engine
{
    public readonly struct FLevelViewportInfo
    {
        public readonly FVector CamPosition;
        public readonly FRotator CamRotation;
        public readonly float CamOrthoZoom;
    }

    public class UWorld : Assets.Exports.UObject
    {
        public FPackageIndex PersistentLevel { get; private set; }
        public FPackageIndex[] ExtraReferencedObjects { get; private set; }
        public FPackageIndex[]? StreamingLevels { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            if (Ar.Game == GAME_WorldofJadeDynasty) Ar.Position += 8;
            base.Deserialize(Ar, validPos);
            PersistentLevel = new FPackageIndex(Ar);
            if (Ar.Ver >= EUnrealEngineObjectUE3Version.WORLD_PERSISTENT_FACEFXANIMSET && Ar.Game < GAME_UE4_0)
            {
                Ar.Position += sizeof(int); // FPackageIndex - PersistentFaceFXAnimSet
            }

            if (Ar.Ver < EUnrealEngineObjectUE4Version.ADD_EDITOR_VIEWS)
            {
                Ar.ReadArray<FLevelViewportInfo>(4); // EditorViews
            }

            if (Ar.Ver < EUnrealEngineObjectUE4Version.REMOVE_SAVEGAMESUMMARY)
            {
                Ar.Position += sizeof(int); // FPackageIndex - SaveGameSummary
            }

            if (Ar.Ver >= EUnrealEngineObjectUE3Version.ADDED_DECAL_MANAGER && Ar.Ver < EUnrealEngineObjectUE3Version.REMOVED_DECAL_MANAGER_FROM_UWORLD)
            {
                Ar.Position += sizeof(int); // FPackageIndex - DecalManager
            }

            if (Ar.Game is GAME_Dishonored) return;
            if (Ar.Ver >= EUnrealEngineObjectUE3Version.ADDED_WORLD_EXTRA_REFERENCED_OBJECTS)
            {
                ExtraReferencedObjects = Ar.ReadArray(() => new FPackageIndex(Ar));
            }
            if (Ar.Game is GAME_AssaultFireFuture && TryGetValue<FPackageIndex>(out var composition, "MiniWorldComposition")) return;
            if (Ar.Game >= GAME_UE4_0)
            {
                StreamingLevels = Ar.ReadArray(() => new FPackageIndex(Ar));
            }

            if (Ar.Game >= GAME_UE4_0 && Ar.Ver < EUnrealEngineObjectUE4Version.REMOVE_CLIENTDESTROYEDACTORCONTENT)
            {
                Ar.ReadArray(() => new FPackageIndex(Ar)); // TempClientDestroyedActorContent
            }
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("PersistentLevel");
            serializer.Serialize(writer, PersistentLevel);

            writer.WritePropertyName("ExtraReferencedObjects");
            serializer.Serialize(writer, ExtraReferencedObjects);

            writer.WritePropertyName("StreamingLevels");
            serializer.Serialize(writer, StreamingLevels);
        }
    }
}
