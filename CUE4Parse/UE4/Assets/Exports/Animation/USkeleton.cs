using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Assets.Exports.Animation;

public class USkeleton : UObject
{
    public EBoneTranslationRetargetingMode[] BoneTree;
    public FReferenceSkeleton ReferenceSkeleton;
    public FGuid Guid;
    public FGuid VirtualBoneGuid;
    public Dictionary<FName, FReferencePose> AnimRetargetSources;
    public Dictionary<FName, FSmartNameMapping> NameMappings;
    public FName[] ExistingMarkerNames;
    public FPackageIndex[] Sockets;
    public FVirtualBone[] VirtualBones;

    public int BoneCount => ReferenceSkeleton.FinalRefBoneInfo.Length;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (Ar.Game == EGame.GAME_WorldofJadeDynasty) Ar.Position += 4;
        // UObject Properties
        if (TryGetValue(out FStructFallback[] boneTree, nameof(BoneTree)))
        {
            BoneTree = new EBoneTranslationRetargetingMode[boneTree.Length];
            for (var i = 0; i < BoneTree.Length; i++)
            {
                BoneTree[i] = boneTree[i].GetOrDefault<EBoneTranslationRetargetingMode>("TranslationRetargetingMode");
            }
        }
        VirtualBoneGuid = GetOrDefault<FGuid>(nameof(VirtualBoneGuid));
        Sockets = GetOrDefault(nameof(Sockets), Array.Empty<FPackageIndex>());
        VirtualBones = GetOrDefault(nameof(VirtualBones), Array.Empty<FVirtualBone>());

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.REFERENCE_SKELETON_REFACTOR)
        {
            ReferenceSkeleton = new FReferenceSkeleton(Ar);
        }

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.FIX_ANIMATIONBASEPOSE_SERIALIZATION)
        {
            var numOfRetargetSources = Ar.Read<int>();
            if (Ar.Game == EGame.GAME_WorldofJadeDynasty) numOfRetargetSources ^= 0x0a8a8fd1;
            AnimRetargetSources = new Dictionary<FName, FReferencePose>(numOfRetargetSources);
            for (var i = 0; i < numOfRetargetSources; i++)
            {
                var name = Ar.ReadFName();
                var pose = new FReferencePose(Ar);
                ReferenceSkeleton.AdjustBoneScales(pose.ReferencePose);
                AnimRetargetSources[name] = pose;
            }
        }
        else
        {
            Log.Warning(""); // not sure what to put here
        }

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.SKELETON_GUID_SERIALIZATION)
        {
            Guid = Ar.Read<FGuid>();
        }

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.SKELETON_ADD_SMARTNAMES)
        {
            bool isLegacy = Ar.Game < EGame.GAME_UE5_8;
            if (isLegacy || (!Ar.IsFilterEditorOnly && FUE5ReleaseStreamObjectVersion.Get(Ar) < FUE5ReleaseStreamObjectVersion.Type.RemovedSmartNameContainerPayload))
            {
                NameMappings = Ar.ReadMap(Ar.ReadFName, () => new FSmartNameMapping(Ar));
            }
        }


        if (FAnimObjectVersion.Get(Ar) >= FAnimObjectVersion.Type.StoreMarkerNamesOnSkeleton)
        {
            var stripDataFlags = new FStripDataFlags(Ar);
            if (!stripDataFlags.IsEditorDataStripped())
            {
                ExistingMarkerNames = Ar.ReadArray(Ar.ReadFName);
            }
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(ReferenceSkeleton));
        serializer.Serialize(writer, ReferenceSkeleton);

        writer.WritePropertyName(nameof(Guid));
        serializer.Serialize(writer, Guid);

        writer.WritePropertyName(nameof(AnimRetargetSources));
        serializer.Serialize(writer, AnimRetargetSources);

        writer.WritePropertyName(nameof(NameMappings));
        serializer.Serialize(writer, NameMappings);

        writer.WritePropertyName(nameof(ExistingMarkerNames));
        serializer.Serialize(writer, ExistingMarkerNames);
    }
}

[StructFallback]
public class FVirtualBone
{
    public FName SourceBoneName;
    public FName TargetBoneName;
    public FName VirtualBoneName;

    public FVirtualBone(FStructFallback fallback)
    {
        SourceBoneName = fallback.GetOrDefault<FName>(nameof(SourceBoneName));
        TargetBoneName = fallback.GetOrDefault<FName>(nameof(TargetBoneName));
        VirtualBoneName = fallback.GetOrDefault<FName>(nameof(VirtualBoneName));
    }
}
