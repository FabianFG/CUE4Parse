using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CUE4Parse_Conversion.Animations.PSA;
using CUE4Parse.ACL;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Animation.ACL;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using static CUE4Parse.UE4.Assets.Exports.Animation.AnimationCompressionFormat;
using static CUE4Parse.UE4.Assets.Exports.Animation.AnimationKeyFormat;
using static CUE4Parse.UE4.Assets.Exports.Animation.AnimationCompressionUtils;

namespace CUE4Parse_Conversion.Animations
{
    public static class AnimConverter
    {
        private static CAnimSet ConvertToAnimSet(this USkeleton skeleton)
        {
            return new CAnimSet(skeleton);
        }

        public static CAnimSet ConvertAnims(this USkeleton skeleton, UAnimComposite? animComposite)
        {
            var animSet = skeleton.ConvertToAnimSet();
            if (animComposite == null) return animSet;

            foreach (var segment in animComposite.AnimationTrack.AnimSegments)
            {
                if (!segment.AnimReference.TryLoad(out UAnimSequence animSequence))
                    continue;

                var seq = animSequence.ConvertSequence(skeleton);
                seq.StartPos = segment.StartPos;
                seq.AnimEndTime = segment.AnimEndTime;
                seq.LoopingCount = segment.LoopingCount;
                animSet.Sequences.Add(seq);
            }

            animSet.TotalAnimTime = animComposite.AnimationTrack.GetLength();
            return animSet;
        }

        public static CAnimSet ConvertAnims(this USkeleton skeleton, UAnimMontage? animMontage)
        {
            var animSet = skeleton.ConvertToAnimSet();
            if (animMontage == null) return animSet;

            foreach (var slotAnimTrack in animMontage.SlotAnimTracks)
            {
                foreach (var segment in slotAnimTrack.AnimTrack.AnimSegments)
                {
                    if (!segment.AnimReference.TryLoad(out UAnimSequence animSequence))
                        continue;

                    var seq = animSequence.ConvertSequence(skeleton);
                    seq.Name = slotAnimTrack.SlotName.Text;
                    seq.StartPos = segment.StartPos;
                    seq.AnimEndTime = segment.AnimEndTime;
                    seq.LoopingCount = segment.LoopingCount;
                    animSet.Sequences.Add(seq);
                }
            }

            // var compositeSection = animMontage.CompositeSections[0];
            // do
            // {
            //     if (compositeSection.LinkedSequence.TryLoad(out UAnimSequence animSequence))
            //     {
            //         var segment = animMontage.SlotAnimTracks[compositeSection.SlotIndex].AnimTrack.AnimSegments[compositeSection.SegmentIndex];
            //         var seq = animSequence.ConvertSequence(skeleton);
            //         seq.Name = compositeSection.SectionName.Text;
            //         seq.StartPos = segment.StartPos;
            //         seq.AnimEndTime = segment.AnimEndTime;
            //         seq.LoopingCount = segment.LoopingCount;
            //         animSet.Sequences.Add(seq);
            //     }
            //
            //     compositeSection = animMontage.CompositeSections.FirstOrDefault(x => x.SectionName == compositeSection.NextSectionName);
            // } while (compositeSection is not null && !compositeSection.NextSectionName.IsNone &&
            //          compositeSection.SectionName != compositeSection.NextSectionName);

            // foreach (var compositeSection in animMontage.CompositeSections)
            // {
            //     var segment = animMontage.SlotAnimTracks[compositeSection.SlotIndex].AnimTrack.AnimSegments[compositeSection.SegmentIndex];
            //     if (!segment.AnimReference.TryLoad(out UAnimSequence animSequence) || !compositeSection.LinkedSequence.TryLoad(out animSequence))
            //         continue;
            //
            //     var seq = animSequence.ConvertSequence(skeleton);
            //     seq.Name = compositeSection.SectionName.Text;
            //     seq.StartPos = segment.StartPos;
            //     seq.AnimEndTime = segment.AnimEndTime;
            //     seq.LoopingCount = segment.LoopingCount;
            //     animSet.Sequences.Add(seq);
            // }

            animSet.TotalAnimTime = animMontage.CalculateSequenceLength();
            return animSet;
        }

        public static CAnimSet ConvertAnims(this USkeleton skeleton, UAnimSequence? animSequence)
        {
            var animSet = skeleton.ConvertToAnimSet();
            if (animSequence == null) return animSet;

            // Store UAnimSequence in 'OriginalAnims' array, we just need it from time to time
            //OriginalAnims.Add(animSequence);

            // Create CAnimSequence
            animSet.Sequences.Add(animSequence.ConvertSequence(skeleton));
            animSet.TotalAnimTime = animSequence.SequenceLength;
            return animSet;
        }

        private static CAnimSequence ConvertSequence(this UAnimSequence animSequence, USkeleton skeleton)
        {
            var animSeq = new CAnimSequence(animSequence, skeleton);

            var numBones = skeleton.BoneCount;
            if (animSequence.RawAnimationData is { Length: > 0 })
            {
                Trace.Assert(animSequence.RawAnimationData.Length == animSeq.Tracks.Capacity);

                static void CopyArray<T>(out T[] dest, T[] src)
                {
                    dest = new T[src.Length];
                    src.CopyTo(dest, 0);
                }

                for (var boneIndex = 0; boneIndex < numBones; boneIndex++)
                {
                    var track = new CAnimTrack();
                    animSeq.Tracks.Add(track);
                    var trackIndex = animSequence.FindTrackForBoneIndex(boneIndex);
                    if (trackIndex >= 0)
                    {
                        CopyArray(out track.KeyQuat, animSequence.RawAnimationData[trackIndex].RotKeys);
                        CopyArray(out track.KeyPos, animSequence.RawAnimationData[trackIndex].PosKeys);
                        CopyArray(out track.KeyScale, animSequence.RawAnimationData[trackIndex].ScaleKeys);
                    }
                }
            }
            else switch (animSequence.CompressedDataStructure)
            {
                case FUECompressedAnimData ueData:
                {
                    // There could be an animation consisting of only trans with offsets == -1, what means
                    // use of RefPose. In this case there's no point adding the animation to AnimSet. We'll
                    // create FMemReader even for empty CompressedByteStream, otherwise it would be hard to
                    // create a valid CAnimSequence which won't crash animation export.
                    using var reader = new FByteArchive("CompressedByteStream", ueData.CompressedByteStream);
                    for (var boneIndex = 0; boneIndex < numBones; boneIndex++)
                    {
                        var track = new CAnimTrack();
                        animSeq.Tracks.Add(track);
                        var trackIndex = animSequence.FindTrackForBoneIndex(boneIndex);
                        if (trackIndex >= 0)
                        {
                            if (ueData.KeyEncodingFormat == AKF_PerTrackCompression)
                                ReadPerTrackData(reader, animSequence, track, trackIndex);
                            else
                                ReadKeyLerpData(reader, animSequence, track, trackIndex, ueData.KeyEncodingFormat == AKF_VariableKeyLerp);
                        }
                    }

                    break;
                }
                case FACLCompressedAnimData aclData:
                {
                    var tracks = aclData.GetCompressedTracks();
                    var tracksHeader = tracks.GetTracksHeader();
                    var numSamples = (int) tracksHeader.NumSamples;

                    // smh Valo has this set to 1, but it should be 0, right?
                    if (animSequence.IsValidAdditive()) tracks.SetDefaultScale(0);

                    // Let the native code do its job
                    var atomKeys = new FTransform[animSeq.Tracks.Capacity * numSamples];
                    unsafe
                    {
                        fixed (FTransform* refPosePtr = animSeq.RetargetBasePose ?? skeleton.ReferenceSkeleton.FinalRefBonePose)
                        fixed (FTrackToSkeletonMap* trackToSkeletonMapPtr = animSequence.GetTrackMap())
                        fixed (FTransform* atomKeysPtr = atomKeys)
                        {
                            nReadACLData(tracks.Handle, refPosePtr, trackToSkeletonMapPtr, atomKeysPtr);
                        }
                    }

                    // Prepare buffers of all samples of each transform property for the native code to populate
                    var posKeys = new FVector[atomKeys.Length];
                    var rotKeys = new FQuat[atomKeys.Length];
                    var scaleKeys = new FVector[atomKeys.Length];
                    for (var i = 0; i < atomKeys.Length; i++)
                    {
                        posKeys[i] = atomKeys[i].Translation;
                        rotKeys[i] = atomKeys[i].Rotation;
                        scaleKeys[i] = atomKeys[i].Scale3D;
                    }

                    // Now create CAnimTracks with the data from those big buffers
                    for (var boneIndex = 0; boneIndex < numBones; boneIndex++)
                    {
                        var track = new CAnimTrack();
                        animSeq.Tracks.Add(track);
                        var trackIndex = animSequence.FindTrackForBoneIndex(boneIndex);
                        if (trackIndex >= 0)
                        {
                            var offset = trackIndex * numSamples;
                            track.KeyPos = new FVector[numSamples];
                            track.KeyQuat = new FQuat[numSamples];
                            track.KeyScale = new FVector[numSamples];
                            Array.Copy(posKeys, offset, track.KeyPos, 0, numSamples);
                            Array.Copy(rotKeys, offset, track.KeyQuat, 0, numSamples);
                            Array.Copy(scaleKeys, offset, track.KeyScale, 0, numSamples);
                        }
                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException("Unsupported compressed data type " + animSequence.CompressedDataStructure.GetType().Name);
            }

            // ok?
            if (animSequence.IsValidAdditive()) animSeq = animSeq.ConvertAdditive(skeleton);
            AdjustSequenceBySkeleton(skeleton.ReferenceSkeleton, animSeq.RetargetBasePose ?? skeleton.ReferenceSkeleton.FinalRefBonePose, animSeq);
            return animSeq;
        }

        private static CAnimSequence ConvertAdditive(this CAnimSequence animSeq, USkeleton skeleton)
            => animSeq.ConvertAdditive(animSeq.OriginalSequence.RefPoseSeq?.Load<UAnimSequence>(), skeleton);
        public static CAnimSequence ConvertAdditive(this CAnimSequence animSeq, UAnimSequence? refPoseSeq, USkeleton skeleton)
        {
            var refFrameIndex = animSeq.OriginalSequence.RefFrameIndex;
            var refPoseSkel = refPoseSeq?.Skeleton.Load<USkeleton>() ?? skeleton;
            var refAnimSet = refPoseSkel.ConvertAnims(refPoseSeq);

            FCompactPose[] additivePoses = FAnimationRuntime.LoadAsPoses(animSeq, skeleton);
            FCompactPose[] referencePoses = animSeq.OriginalSequence.RefPoseType switch
            {
                EAdditiveBasePoseType.ABPT_RefPose => FAnimationRuntime.LoadRestAsPoses(skeleton),
                EAdditiveBasePoseType.ABPT_AnimScaled => FAnimationRuntime.LoadAsPoses(refAnimSet.Sequences[0], refPoseSkel),
                EAdditiveBasePoseType.ABPT_AnimFrame => FAnimationRuntime.LoadAsPoses(refAnimSet.Sequences[0], refPoseSkel, refFrameIndex),
                EAdditiveBasePoseType.ABPT_LocalAnimFrame => FAnimationRuntime.LoadAsPoses(animSeq, skeleton, refFrameIndex),
                _ => throw new ArgumentOutOfRangeException("Unsupported additive type " + animSeq.OriginalSequence.RefPoseType)
            };

            // reset tracks and their size to avoid empty additive track on filled ref track
            // or the other way around, that way we are sure all tracks can receive all frames
            animSeq.Tracks = new List<CAnimTrack>(additivePoses[0].Bones.Length);
            for (int i = 0; i < additivePoses[0].Bones.Length; i++)
            {
                animSeq.Tracks.Add(new CAnimTrack(additivePoses.Length));
            }

            var maxRefPosFrame = referencePoses.Length;
            for (var frameIndex = 0; frameIndex < additivePoses.Length; frameIndex++)
            {
                var addPose = additivePoses[frameIndex];
                var refPose = (FCompactPose)referencePoses[animSeq.OriginalSequence.RefPoseType switch
                {
                    EAdditiveBasePoseType.ABPT_AnimScaled => frameIndex % maxRefPosFrame,
                    _ => refFrameIndex
                }].Clone();

                switch (animSeq.OriginalSequence.AdditiveAnimType)
                {
                    case EAdditiveAnimationType.AAT_LocalSpaceBase:
                        FAnimationRuntime.AccumulateLocalSpaceAdditivePoseInternal(refPose, addPose, 1);
                        break;
                    case EAdditiveAnimationType.AAT_RotationOffsetMeshSpace:
                        FAnimationRuntime.AccumulateMeshSpaceRotationAdditiveToLocalPoseInternal(refPose, addPose, 1);
                        break;
                }

                refPose.PushTransformAtFrame(animSeq.Tracks, frameIndex);
            }

            if (refPoseSeq != null) // for FindTrackForBoneIndex
                animSeq.OriginalSequence = refAnimSet.Sequences[0].OriginalSequence;
            return animSeq;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AdjustSequenceBySkeleton(FReferenceSkeleton skeleton, FTransform[] transforms, CAnimSequence anim)
        {
            if (skeleton.FinalRefBoneInfo.Length == 0 ||
                skeleton.FinalRefBoneInfo.Length != transforms.Length)
                return;

            for (var boneIndex = 0; boneIndex < transforms.Length; boneIndex++)
            {
                var boneScale = skeleton.GetBoneScale(transforms, boneIndex);
                if (Math.Abs(boneScale.X - 1.0f) > 0.001f ||
                    Math.Abs(boneScale.Y - 1.0f) > 0.001f ||
                    Math.Abs(boneScale.Z - 1.0f) > 0.001f)
                {
                    var track = anim.Tracks[boneIndex]; // tracks are bone indexed
                    for (int keyIndex = 0; keyIndex < track.KeyPos.Length; keyIndex++)
                    {
                        // Scale translation by accumulated bone scale value
                        track.KeyPos[keyIndex].Scale(boneScale);
                    }
                }
            }
        }

        private static void ReadTimeArray(FArchive Ar, int numKeys, out float[] times, int numFrames)
        {
            times = new float[numKeys];
            if (numKeys <= 1) return;

            if (numFrames < 256)
            {
                for (var keyIndex = 0; keyIndex < numKeys; keyIndex++)
                {
                    var v = Ar.Read<byte>();
                    times[keyIndex] = v;
                }
            }
            else
            {
                for (var k = 0; k < numKeys; k++)
                {
                    var keyIndex = Ar.Read<ushort>();
                    times[k] = keyIndex;
                }
            }

            // align to 4 bytes
            Ar.Position = Ar.Position.Align(4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadPerTrackQuatData(FArchive Ar, string trackKind, ref FQuat[] dstKeys, ref float[] dstTimeKeys, int numFrames)
        {
            var packedInfo = Ar.Read<uint>();
            var keyFormat = (AnimationCompressionFormat) (packedInfo >> 28);
            var componentMask = (int) ((packedInfo >> 24) & 0xF);
            var numKeys = (int) (packedInfo & 0xFFFFFF);
            var hasTimeTracks = (componentMask & 8) != 0;

            var mins = FVector.ZeroVector;
            var ranges = FVector.ZeroVector;

            dstKeys = new FQuat[numKeys];
            if (keyFormat == ACF_IntervalFixed32NoW)
            {
                // read mins/maxs
                if ((componentMask & 1) != 0)
                {
                    mins.X = Ar.Read<float>();
                    ranges.X = Ar.Read<float>();
                }
                if ((componentMask & 2) != 0)
                {
                    mins.Y = Ar.Read<float>();
                    ranges.Y = Ar.Read<float>();
                }
                if ((componentMask & 4) != 0)
                {
                    mins.Z = Ar.Read<float>();
                    ranges.Z = Ar.Read<float>();
                }
            }
            for (var keyIndex = 0; keyIndex < numKeys; keyIndex++)
            {
                dstKeys[keyIndex] = keyFormat switch
                {
                    ACF_None or ACF_Float96NoW => Ar.ReadQuatFloat96NoW(),
                    ACF_Fixed48NoW => Ar.ReadQuatFixed48NoW(componentMask),
                    ACF_Fixed32NoW => Ar.ReadQuatFixed32NoW(),
                    ACF_IntervalFixed32NoW => Ar.ReadQuatIntervalFixed32NoW(mins, ranges),
                    ACF_Float32NoW => Ar.ReadQuatFloat32NoW(),
                    ACF_Identity => FQuat.Identity,
                    _ => throw new ParserException(Ar, $"Unknown {trackKind} compression method: {(int) keyFormat} ({keyFormat})")
                };
            }
            // align to 4 bytes
            Ar.Position = Ar.Position.Align(4);
            if (hasTimeTracks)
                ReadTimeArray(Ar, numKeys, out dstTimeKeys, numFrames);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadPerTrackVectorData(FArchive Ar, string trackKind, ref FVector[] dstKeys, ref float[] dstTimeKeys, int numFrames)
        {
            var packedInfo = Ar.Read<uint>();
            var keyFormat = (AnimationCompressionFormat) (packedInfo >> 28);
            var componentMask = (int) ((packedInfo >> 24) & 0xF);
            var numKeys = (int) (packedInfo & 0xFFFFFF);
            var hasTimeTracks = (componentMask & 8) != 0;

            var mins = FVector.ZeroVector;
            var ranges = FVector.ZeroVector;

            dstKeys = new FVector[numKeys];
            if (keyFormat == ACF_IntervalFixed32NoW)
            {
                // read mins/maxs
                if ((componentMask & 1) != 0)
                {
                    mins.X = Ar.Read<float>();
                    ranges.X = Ar.Read<float>();
                }
                if ((componentMask & 2) != 0)
                {
                    mins.Y = Ar.Read<float>();
                    ranges.Y = Ar.Read<float>();
                }
                if ((componentMask & 4) != 0)
                {
                    mins.Z = Ar.Read<float>();
                    ranges.Z = Ar.Read<float>();
                }
            }
            for (var keyIndex = 0; keyIndex < numKeys; keyIndex++)
            {
                switch (keyFormat)
                {
                    case ACF_None:
                    case ACF_Float96NoW:
                    {
                        FVector v;
                        if ((componentMask & 7) != 0)
                        {
                            v = new FVector(
                                (componentMask & 1) != 0 ? Ar.Read<float>() : 0,
                                (componentMask & 2) != 0 ? Ar.Read<float>() : 0,
                                (componentMask & 4) != 0 ? Ar.Read<float>() : 0
                            );
                        }
                        else
                        {
                            // ACF_Float96NoW has a special case for ((ComponentMask & 7) == 0)
                            v = Ar.Read<FVector>();
                        }
                        dstKeys[keyIndex] = v;
                        break;
                    }
                    case ACF_IntervalFixed32NoW:
                    {
                        var v = Ar.ReadVectorIntervalFixed32(mins, ranges);
                        dstKeys[keyIndex] = v;
                        break;
                    }
                    case ACF_Fixed48NoW:
                    {
                        var v = new FVector(
                            (componentMask & 1) != 0 ? DecodeFixed48_PerTrackComponent(Ar.Read<ushort>(), 7) : 0,
                            (componentMask & 2) != 0 ? DecodeFixed48_PerTrackComponent(Ar.Read<ushort>(), 7) : 0,
                            (componentMask & 4) != 0 ? DecodeFixed48_PerTrackComponent(Ar.Read<ushort>(), 7) : 0
                        );
                        dstKeys[keyIndex] = v;
                        break;
                    }
                    case ACF_Identity:
                        dstKeys[keyIndex] = FVector.ZeroVector;
                        break;
                    default:
                        throw new ParserException(Ar, $"Unknown {trackKind} compression method: {(int) keyFormat} ({keyFormat})");
                }
            }
            // align to 4 bytes
            Ar.Position = Ar.Position.Align(4);
            if (hasTimeTracks)
                ReadTimeArray(Ar, numKeys, out dstTimeKeys, numFrames);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadPerTrackData(FArchive reader, UAnimSequence animSequence, CAnimTrack track, int trackIndex)
        {
            var compressedData = (FUECompressedAnimData) animSequence.CompressedDataStructure;

            // this format uses different key storage
            Trace.Assert(compressedData.TranslationCompressionFormat == ACF_Identity);
            Trace.Assert(compressedData.RotationCompressionFormat == ACF_Identity);

            var transOffset = compressedData.CompressedTrackOffsets[trackIndex * 2];
            var rotOffset = compressedData.CompressedTrackOffsets[trackIndex * 2 + 1];
            var scaleOffset = compressedData.CompressedScaleOffsets.IsValid() ? compressedData.CompressedScaleOffsets.OffsetData[trackIndex] : -1;

            // read translation keys
            if (transOffset == -1)
            {
                track.KeyPos = new[] { FVector.ZeroVector };
            }
            else
            {
                reader.Position = transOffset;
                ReadPerTrackVectorData(reader, "translation", ref track.KeyPos, ref track.KeyPosTime, animSequence.NumFrames);
            }

            // read rotation keys
            if (rotOffset == -1)
            {
                track.KeyQuat = new[] { FQuat.Identity };
            }
            else
            {
                reader.Position = rotOffset;
                ReadPerTrackQuatData(reader, "rotation", ref track.KeyQuat, ref track.KeyQuatTime, animSequence.NumFrames);
            }

            // read scale keys
            if (scaleOffset != -1)
            {
                reader.Position = scaleOffset;
                ReadPerTrackVectorData(reader, "scale", ref track.KeyScale, ref track.KeyScaleTime, animSequence.NumFrames);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadKeyLerpData(FArchive reader, UAnimSequence animSequence, CAnimTrack track, int trackIndex, bool hasTimeTracks)
        {
            var compressedData = (FUECompressedAnimData) animSequence.CompressedDataStructure;
            var transOffset = compressedData.CompressedTrackOffsets[trackIndex * 4];
            var transKeys = compressedData.CompressedTrackOffsets[trackIndex * 4 + 1];
            var rotOffset = compressedData.CompressedTrackOffsets[trackIndex * 4 + 2];
            var rotKeys = compressedData.CompressedTrackOffsets[trackIndex * 4 + 3];

            track.KeyPos = new FVector[transKeys];
            track.KeyQuat = new FQuat[rotKeys];

            var mins = FVector.ZeroVector;
            var ranges = FVector.ZeroVector;

            // read translation keys
            if (transKeys > 0)
            {
                reader.Position = transOffset;
                var translationCompressionFormat = compressedData.TranslationCompressionFormat;
                if (transKeys == 1)
                    translationCompressionFormat = ACF_None; // single key is stored without compression
                // read mins/ranges
                if (translationCompressionFormat == ACF_IntervalFixed32NoW)
                {
                    mins = reader.Read<FVector>();
                    ranges = reader.Read<FVector>();
                }

                for (var keyIndex = 0; keyIndex < transKeys; keyIndex++)
                {
                    track.KeyPos[keyIndex] = translationCompressionFormat switch
                    {
                        ACF_None => reader.Read<FVector>(),
                        ACF_Float96NoW => reader.Read<FVector>(),
                        ACF_IntervalFixed32NoW => reader.ReadVectorIntervalFixed32(mins, ranges),
                        ACF_Fixed48NoW => reader.ReadVectorFixed48(),
                        ACF_Identity => FVector.ZeroVector,
                        _ => throw new ParserException($"Unknown translation compression method: {(int) translationCompressionFormat} ({translationCompressionFormat})")
                    };
                }

                // align to 4 bytes
                reader.Position = reader.Position.Align(4);
                if (hasTimeTracks)
                    ReadTimeArray(reader, transKeys, out track.KeyPosTime, animSequence.NumFrames);
            }
            else
            {
                // A.KeyPos.Add(FVector.ZeroVector);
                // appNotify("No translation keys!");
            }

            // read rotation keys
            reader.Position = rotOffset;
            var rotationCompressionFormat = compressedData.RotationCompressionFormat;

            switch (rotKeys)
            {
                case 1:
                    rotationCompressionFormat = ACF_Float96NoW; // single key is stored without compression
                    break;
                case > 1 when rotationCompressionFormat == ACF_IntervalFixed32NoW:
                    // Mins/Ranges are read only when needed - i.e. for ACF_IntervalFixed32NoW
                    mins = reader.Read<FVector>();
                    ranges = reader.Read<FVector>();
                    break;
            }

            for (var k = 0; k < rotKeys; k++)
            {
                track.KeyQuat[k] = rotationCompressionFormat switch
                {
                    ACF_None => reader.Read<FQuat>(),
                    ACF_Float96NoW => reader.ReadQuatFloat96NoW(),
                    ACF_Fixed48NoW => reader.ReadQuatFixed48NoW(),
                    ACF_Fixed32NoW => reader.ReadQuatFixed32NoW(),
                    ACF_IntervalFixed32NoW => reader.ReadQuatIntervalFixed32NoW(mins, ranges),
                    ACF_Float32NoW => reader.ReadQuatFloat32NoW(),
                    ACF_Identity => FQuat.Identity,
                    _ => throw new ParserException($"Unknown rotation compression method: {(int) rotationCompressionFormat} ({rotationCompressionFormat})")
                };
            }

            if (hasTimeTracks)
            {
                // align to 4 bytes
                reader.Position = reader.Position.Align(4);
                ReadTimeArray(reader, rotKeys, out track.KeyQuatTime, animSequence.NumFrames);
            }
        }

        [DllImport(ACLNative.LIB_NAME)]
        private static extern unsafe void nReadACLData(IntPtr compressedTracks, FTransform* inRefPoses, FTrackToSkeletonMap* inTrackToSkeletonMap, FTransform* outAtom);
    }
}
