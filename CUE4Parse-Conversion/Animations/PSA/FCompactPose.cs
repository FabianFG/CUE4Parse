using System;
using System.Collections.Generic;
using System.Diagnostics;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Animations.PSA
{
    public class FCompactPose : ICloneable
    {
        public readonly FPoseBone[] Bones;

        public FCompactPose(int boneLength)
        {
            Bones = new FPoseBone[boneLength];
        }

        public void NormalizeRotations()
        {
            foreach (var bone in Bones)
                bone.Transform.Rotation.Normalize();
        }

        public void PushTransformAtFrame(List<CAnimTrack> dstTracks, int frame)
        {
            Debug.Assert(dstTracks.Count == Bones.Length);

            for (int index = 0; index < Bones.Length; ++index)
            {
                if (!Bones[index].IsValidKey) continue;

                FTransform transform = Bones[index].Transform;
                dstTracks[index].KeyQuat[frame] = transform.Rotation;
                dstTracks[index].KeyPos[frame] = transform.Translation;
                dstTracks[index].KeyScale[frame] = transform.Scale3D;
            }
        }

        public object Clone()
        {
            var pose = new FCompactPose(Bones.Length);
            for (int i = 0; i < pose.Bones.Length; i++)
            {
                pose.Bones[i] = (FPoseBone)Bones[i].Clone();
            }
            return pose;
        }
    }
}
