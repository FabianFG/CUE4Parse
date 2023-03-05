using System.Collections.Generic;
using System.Diagnostics;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Animations.PSA
{
    public class FCompactPose
    {
        public readonly FPoseBone[] Bones;
        public int AnimFrame;
        public bool Processed;

        public FCompactPose(FReferenceSkeleton refSkel)
        {
            Bones = new FPoseBone[refSkel.FinalRefBoneInfo.Length];
        }

        public void NormalizeRotations()
        {
            foreach (var bone in Bones)
                bone.Transform.Rotation.Normalize();
        }

        public void AddToTracks(List<CAnimTrack> tracks)
        {
            Debug.Assert(tracks.Count == Bones.Length);

            for (int index = 0; index < Bones.Length; ++index)
            {
                if (!Bones[index].IsValidKey) continue;

                FTransform transform = Bones[index].Transform;
                tracks[index].KeyQuat[AnimFrame] = transform.Rotation;
                tracks[index].KeyPos[AnimFrame] = transform.Translation;
                tracks[index].KeyScale[AnimFrame] = transform.Scale3D;
            }
        }
    }
}
