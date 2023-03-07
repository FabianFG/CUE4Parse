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

        public void AddToTracks(List<CAnimTrack> tracks, int frame)
        {
            Debug.Assert(tracks.Count == Bones.Length);

            for (int index = 0; index < Bones.Length; ++index)
            {
                if (!Bones[index].IsValidKey) continue;

                FTransform transform = Bones[index].Transform;
                tracks[index].KeyQuat[frame] = transform.Rotation;
                tracks[index].KeyPos[frame] = transform.Translation;
                tracks[index].KeyScale[frame] = transform.Scale3D;
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
