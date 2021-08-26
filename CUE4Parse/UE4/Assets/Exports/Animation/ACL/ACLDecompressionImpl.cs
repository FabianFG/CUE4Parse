using System;
using System.Runtime.CompilerServices;
using CUE4Parse.ACL;

namespace CUE4Parse.UE4.Assets.Exports.Animation.ACL
{
    public static class ACLDecompressionImpl
    {
        /** These 3 indices map into the output Atom array. */
        public struct FAtomIndices
        {
            public ushort Rotation;
            public ushort Translation;
            public ushort Scale;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SampleRoundingPolicy GetRoundingPolicy(EAnimInterpolationType interpType) => interpType == EAnimInterpolationType.Step ? SampleRoundingPolicy.Floor : SampleRoundingPolicy.None;

        public static void DecompressBone(FAnimSequenceDecompressionContext decompContext, DecompressionContext aclContext, int trackIndex, out FTransform outAtom)
        {
            aclContext.Seek(decompContext.Time, GetRoundingPolicy(decompContext.Interpolation));
            aclContext.DecompressTrack(trackIndex, out outAtom);
        }

        public static void DecompressPose(FAnimSequenceDecompressionContext decompContext, DecompressionContext aclContext, BoneTrackPair[] rotationPairs, BoneTrackPair[] translationPairs, BoneTrackPair[] scalePairs, FTransform[] outAtoms)
        {
            aclContext.Seek(decompContext.Time, GetRoundingPolicy(decompContext.Interpolation));

            var compressedClipData = aclContext.GetCompressedTracks()!;
            var tracksHeader = compressedClipData.GetTracksHeader();
            var aclBoneCount = tracksHeader.NumTracks;
            var trackToAtomsMap = new FAtomIndices[aclBoneCount];
            for (int i = 0; i < aclBoneCount; i++)
            {
                trackToAtomsMap[i] = new FAtomIndices { Rotation = 0xFF, Translation = 0xFF, Scale = 0xFF };
            }

            foreach (ref readonly var pair in rotationPairs.AsSpan())
            {
                trackToAtomsMap[pair.TrackIndex].Rotation = (ushort) pair.AtomIndex;
            }

            foreach (ref readonly var pair in translationPairs.AsSpan())
            {
                trackToAtomsMap[pair.TrackIndex].Translation = (ushort) pair.AtomIndex;
            }

            if (tracksHeader.GetHasScale())
            {
                foreach (ref readonly var pair in scalePairs.AsSpan())
                {
                    trackToAtomsMap[pair.TrackIndex].Scale = (ushort) pair.AtomIndex;
                }
            }
        }
    }
}