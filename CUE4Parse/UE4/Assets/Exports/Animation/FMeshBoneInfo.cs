using CUE4Parse.UE4.Assets.Exports.ActorX;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    [JsonConverter(typeof(FMeshBoneInfoConverter))]
    public struct FMeshBoneInfo
    {
        public readonly FName Name;
        public readonly int ParentIndex;
        public readonly VJointPosPsk BonePos;

        public FMeshBoneInfo(FAssetArchive Ar)
        {
            Name = Ar.ReadFName();
            if (Ar.Game < EGame.GAME_UE4_0)
            {
                Ar.Read<int>(); // reserved Flags
                BonePos = new VJointPosPsk(Ar);
                Ar.Read<int>(); // NumChildren
            }
            ParentIndex = Ar.Read<int>();

            if (Ar.Ver >= EUnrealEngineObjectUE3Version.SKELMESH_DRAWSKELTREEMANAGER && Ar.Ver < EUnrealEngineObjectUE4Version.REFERENCE_SKELETON_REFACTOR)
            {
                Ar.Read<FColor>(); // BoneColor
            }

            if (Ar.Ver >= EUnrealEngineObjectUE4Version.STORE_BONE_EXPORT_NAMES && !Ar.IsFilterEditorOnly)
            {
                Ar.SkipFString(); // ExportName
            }
        }

        public FMeshBoneInfo(FName name, int parentIndex)
        {
            Name = name;
            ParentIndex = parentIndex;
        }

        public override string ToString() => $"{Name}";
    }
}
