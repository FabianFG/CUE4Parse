using System.Collections.Generic;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.Engine.Animation
{
    public class UPoseAsset : UAnimationAsset
    {
        public FPoseDataContainer PoseContainer;
        public bool bAdditivePose;
        public int BasePoseIndex;
        public FName RetargetSource;
        public FTransform[] RetargetSourceAssetReferencePose;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            
            PoseContainer = GetOrDefault<FPoseDataContainer>(nameof(PoseContainer));
            bAdditivePose = GetOrDefault<bool>(nameof(bAdditivePose));
            BasePoseIndex = GetOrDefault<int>(nameof(BasePoseIndex));
            RetargetSource = GetOrDefault<FName>(nameof(RetargetSource));
            RetargetSourceAssetReferencePose = GetOrDefault<FTransform[]>(nameof(RetargetSourceAssetReferencePose));
        }
    }

    [StructFallback]
    public class FPoseData
    {
        public FTransform[] LocalSpacePose;
        public float[] CurveData;

        public FPoseData(FStructFallback fallback)
        {
            LocalSpacePose = fallback.GetOrDefault<FTransform[]>(nameof(LocalSpacePose));
            CurveData = fallback.GetOrDefault<float[]>(nameof(CurveData));
        }
    }

    [StructFallback]
    public class FPoseAssetInfluence
    {
        public int BoneTransformIndex;
        public int PoseIndex;

        public FPoseAssetInfluence(FStructFallback fallback)
        {
            BoneTransformIndex = fallback.GetOrDefault<int>(nameof(BoneTransformIndex));
            PoseIndex = fallback.GetOrDefault<int>(nameof(PoseIndex));
        }
    }

    [StructFallback]
    public class FPoseAssetInfluences
    {
        public FPoseAssetInfluence[] Influences;

        public FPoseAssetInfluences(FStructFallback fallback)
        {
            Influences = fallback.GetOrDefault<FPoseAssetInfluence[]>(nameof(Influences));
        }
    }

    [StructFallback]
    public class FPoseDataContainer
    {
        public FSmartName[]? PoseNames_DEPRECATED;
        public FName[]? PoseFNames;
        public FName[] Tracks;
        public FPoseAssetInfluences[] TrackPoseInfluenceIndices;
        public FPoseData[] Poses;
        public FAnimCurveBase[] Curves;

        public FPoseDataContainer(FStructFallback fallback)
        {
            PoseNames_DEPRECATED = fallback.GetOrDefault<FSmartName[]>("PoseNames"); // FUE5MainStreamObjectVersion.Type.AnimationRemoveSmartNames
            PoseFNames = fallback.GetOrDefault<FName[]>(nameof(PoseFNames));
            Tracks = fallback.GetOrDefault<FName[]>(nameof(Tracks));
            TrackPoseInfluenceIndices = fallback.GetOrDefault<FPoseAssetInfluences[]>(nameof(TrackPoseInfluenceIndices));
            Poses = fallback.GetOrDefault<FPoseData[]>(nameof(Poses));
            Curves = fallback.GetOrDefault<FAnimCurveBase[]>(nameof(Curves));
        }

        public IEnumerable<string> GetPoseNames()
        {
            if (PoseNames_DEPRECATED is not null)
            {
                return PoseNames_DEPRECATED.Select(x => x.DisplayName.Text);
            }
            
            if (PoseFNames is not null)
            {
                return PoseFNames.Select(x => x.Text);
            }

            return Enumerable.Empty<string>();
        }
    }
}
