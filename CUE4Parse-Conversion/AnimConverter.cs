using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CUE4Parse.ACL;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Animation.ACL;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using static CUE4Parse.UE4.Assets.Exports.Animation.AnimationCompressionFormat;
using static CUE4Parse.UE4.Assets.Exports.Animation.AnimationKeyFormat;
using static CUE4Parse.UE4.Assets.Exports.Animation.EAdditiveAnimationType;
using static CUE4Parse.UE4.Assets.Exports.Animation.AnimationCompressionUtils;

// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

namespace CUE4Parse_Conversion
{
    public class CAnimTrack
    {
        public FQuat[] KeyQuat = Array.Empty<FQuat>();
        public FVector[] KeyPos = Array.Empty<FVector>();
        public FVector[] KeyScale = Array.Empty<FVector>();

        // 3 time arrays; should be used either KeyTime or KeyQuatTime + KeyPosTime
        // When the corresponding array is empty, it will assume that Array[i] == i
        public float[] KeyTime = Array.Empty<float>();
        public float[] KeyQuatTime = Array.Empty<float>();
        public float[] KeyPosTime = Array.Empty<float>();
        public float[] KeyScaleTime = Array.Empty<float>();

        // DstPos and/or DstQuat will not be changed when KeyPos and/or KeyQuat are empty.
        public void GetBonePosition(float frame, float numFrames, bool loop, ref FVector dstPos, ref FQuat dstQuat)
        {
            // fast case: 1 frame only
            if (KeyTime.Length == 1 || numFrames == 1 || frame == 0)
            {
                if (KeyPos.Length > 0) dstPos = KeyPos[0];
                if (KeyQuat.Length > 0) dstQuat = KeyQuat[0];
                return;
            }

            // data for lerping
            int posX, rotX; // index of previous frame
            int posY, rotY; // index of next frame
            float posF, rotF; // fraction between X and Y for lerping

            var numTimeKeys = KeyTime.Length;
            var numPosKeys = KeyPos.Length;
            var numRotKeys = KeyQuat.Length;

            if (numTimeKeys > 0)
            {
                // here: KeyPos and KeyQuat sizes either equals to 1 or equals to KeyTime size
                Trace.Assert(numPosKeys <= 1 || numPosKeys == numTimeKeys);
                Trace.Assert(numRotKeys == 1 || numRotKeys == numTimeKeys);

                GetKeyParams(KeyTime, frame, numFrames, loop, out posX, out posY, out posF);
                rotX = posX;
                rotY = posY;
                rotF = posF;

                if (numPosKeys <= 1)
                {
                    posX = posY = 0;
                    posF = 0;
                }
                if (numRotKeys == 1)
                {
                    rotX = rotY = 0;
                    rotF = 0;
                }
            }
            else
            {
                // empty KeyTime array - keys are evenly spaced on a time line
                // note: KeyPos and KeyQuat sizes can be different
                if (KeyPosTime.Length > 0)
                {
                    GetKeyParams(KeyPosTime, frame, numFrames, loop, out posX, out posY, out posF);
                }
                else if (numPosKeys > 1)
                {
                    var position = frame / numFrames * numPosKeys;
                    posX = position.FloorToInt();
                    posF = position - posX;
                    posY = posX + 1;
                    if (posY >= numPosKeys)
                    {
                        if (!loop)
                        {
                            posY = numPosKeys - 1;
                            posF = 0;
                        }
                        else
                            posY = 0;
                    }
                }
                else
                {
                    posX = posY = 0;
                    posF = 0;
                }

                if (KeyQuatTime.Length > 0)
                {
                    GetKeyParams(KeyQuatTime, frame, numFrames, loop, out rotX, out rotY, out rotF);
                }
                else if (numRotKeys > 1)
                {
                    var Position = frame / numFrames * numRotKeys;
                    rotX = Position.FloorToInt();
                    rotF = Position - rotX;
                    rotY = rotX + 1;
                    if (rotY >= numRotKeys)
                    {
                        if (!loop)
                        {
                            rotY = numRotKeys - 1;
                            rotF = 0;
                        }
                        else
                            rotY = 0;
                    }
                }
                else
                {
                    rotX = rotY = 0;
                    rotF = 0;
                }
            }

            // get position
            if (posF > 0)
                dstPos = MathUtils.Lerp(KeyPos[posX], KeyPos[posY], posF);
            else if (numPosKeys > 0) // do not change DstPos when no keys
                dstPos = KeyPos[posX];
            // get orientation
            if (rotF > 0)
                dstQuat = FQuat.Slerp(KeyQuat[rotX], KeyQuat[rotY], rotF);
            else if (numRotKeys > 0) // do not change DstQuat when no keys
                dstQuat = KeyQuat[rotX];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasKeys() => KeyQuat.Length + KeyPos.Length + KeyScale.Length > 0;

        private const int MAX_LINEAR_KEYS = 4;

        private static int FindTimeKey(float[] keyTime, float frame)
        {
            // find index in time key array
            var numKeys = keyTime.Length;
            // *** binary search ***
            int low = 0, high = numKeys-1;
            while (low + MAX_LINEAR_KEYS < high)
            {
                var mid = (low + high) / 2;
                if (frame < keyTime[mid])
                    high = mid-1;
                else
                    low = mid;
            }
            // *** linear search ***
            int i;
            for (i = low; i <= high; i++)
            {
                var currKeyTime = keyTime[i];
                if (frame == currKeyTime)
                    return i;		// exact key
                if (frame < currKeyTime)
                    return i > 0 ? i - 1 : 0;	// previous key
            }
            if (i > high)
                i = high;
            return i;
        }

        // In:  KeyTime, Frame, NumFrames, Loop
        // Out: X - previous key index, Y - next key index, F - fraction between keys
        private static void GetKeyParams(float[] keyTime, float frame, float numFrames, bool loop, out int x, out int y, out float f)
        {
            x = FindTimeKey(keyTime, frame);
            y = x + 1;
            var numTimeKeys = keyTime.Length;
            if (y >= numTimeKeys)
            {
                if (!loop)
                {
                    // clamp animation
                    y = numTimeKeys - 1;
                    Trace.Assert(x == y);
                    f = 0;
                }
                else
                {
                    // loop animation
                    y = 0;
                    f = (frame - keyTime[x]) / (numFrames - keyTime[x]);
                }
            }
            else
            {
                f = (frame - keyTime[x]) / (keyTime[y] - keyTime[x]);
            }
        }
    }

    // Local analog of FTransform
    public struct CSkeletonBonePosition
    {
        public FVector Position;
        public FQuat Orientation;
    }

    public class CAnimSequence
    {
        public string Name; // sequence's name
        public int NumFrames;
        public float Rate;
        public List<CAnimTrack> Tracks; // for each CAnimSet.TrackBoneNames
        public bool bAdditive; // used just for on-screen information
        public UAnimSequence OriginalSequence;
        public CSkeletonBonePosition[] RetargetBasePose;

        public CAnimSequence(UAnimSequence originalSequence)
        {
            OriginalSequence = originalSequence;
        }
    }

    public enum EBoneRetargetingMode : byte
    {
        // Use translation from animation
        Animation,
        // Use translation from mesh
        Mesh,
        AnimationScaled,
        AnimationRelative,
        // Recompute translation from difference between mesh and animation skeletons
        OrientAndScale,

        Count
    }

    public class CAnimSet
    {
        public UObject OriginalAnim;
        public FName[] TrackBoneNames;
        public CSkeletonBonePosition[] BonePositions; // may be empty (for pre-UE4), position in array matches TrackBoneNames
        public List<CAnimSequence> Sequences = new();

        public EBoneRetargetingMode[] BoneModes;

        public CAnimSet() { }

        public CAnimSet(UObject original)
        {
            OriginalAnim = original;
        }

        /** Make a copy of CAnimSet, except animations */
        public void CopyAllButSequences(CAnimSet other)
        {
            OriginalAnim = other.OriginalAnim;
            TrackBoneNames = (FName[]) other.TrackBoneNames.Clone();
            BoneModes = (EBoneRetargetingMode[]) other.BoneModes.Clone();
        }

        // If Skeleton has at most this number of animations, export them as separate psa files.
        // This is needed because UAnimSequence4 can refer to other animation sequences in properties
        // (e.g. UAnimSequence4::RefPoseSeq).
        //private const int MIN_ANIMSET_SIZE = 4; TODO multiple animations per skeleton

        public UObject GetPrimaryAnimObject()
        {
            // When AnimSet consists of just 1 animation track, it is possible that we're exporting
            // a separate UE4 AnimSequence. In this case it's worth using that AnimSequence's filename,
            // otherwise we'll have multiple animations mapped to the same exported file.
            if (Sequences.Count > 0 && OriginalAnim is USkeleton skeleton)
            {
                /*Trace.Assert(skeleton.OriginalAnims.Count == Sequences.Count);
                // Allow up to 3
                if (skeleton.OriginalAnims.Count <= MIN_ANIMSET_SIZE)
                    return skeleton.OriginalAnims[0];*/
                return Sequences[0].OriginalSequence;
            }

            // Not a Skeleton, or has different animation track count
            return OriginalAnim;
        }
    }

    public static class AnimConverter
    {
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

            if (rotKeys == 1)
            {
                rotationCompressionFormat = ACF_Float96NoW; // single key is stored without compression
            }
            else if (rotKeys > 1 && rotationCompressionFormat == ACF_IntervalFixed32NoW)
            {
                // Mins/Ranges are read only when needed - i.e. for ACF_IntervalFixed32NoW
                mins = reader.Read<FVector>();
                ranges = reader.Read<FVector>();
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
        private static extern unsafe void nReadACLData(IntPtr compressedTracks, FVector* outTranslations, FQuat* outRotations, FVector* outScales);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FixRotationKeys(CAnimSequence anim)
        {
            for (var trackIndex = 0; trackIndex < anim.Tracks.Count; trackIndex++)
            {
                if (trackIndex == 0) continue; // don't fix root track
                var track = anim.Tracks[trackIndex];
                for (var keyQuatIndex = 0; keyQuatIndex < track.KeyQuat.Length; keyQuatIndex++)
                {
                    track.KeyQuat[keyQuatIndex].Conjugate();
                }
            }
        }

        public static CAnimSet ConvertAnims(this USkeleton skeleton, UAnimSequence? animSequence)
        {
            var animSet = new CAnimSet(skeleton);

            // Copy bone names
            var numBones = skeleton.ReferenceSkeleton.FinalRefBoneInfo.Length;
            Trace.Assert(skeleton.BoneTree.Length == numBones);

            animSet.TrackBoneNames = new FName[numBones];
            animSet.BonePositions = new CSkeletonBonePosition[numBones];
            animSet.BoneModes = new EBoneRetargetingMode[numBones];

            for (var boneIndex = 0; boneIndex < numBones; boneIndex++)
            {
                // Store bone name
                animSet.TrackBoneNames[boneIndex] = skeleton.ReferenceSkeleton.FinalRefBoneInfo[boneIndex].Name;
                // Store skeleton's bone transform
                CSkeletonBonePosition bonePosition;
                var transform = skeleton.ReferenceSkeleton.FinalRefBonePose[boneIndex];
                bonePosition.Position = transform.Translation;
                bonePosition.Orientation = transform.Rotation;
                animSet.BonePositions[boneIndex] = bonePosition;
                // Process bone retargeting mode
                var boneMode = skeleton.BoneTree[boneIndex].TranslationRetargetingMode switch
                {
                    EBoneTranslationRetargetingMode.Skeleton => EBoneRetargetingMode.Mesh,
                    EBoneTranslationRetargetingMode.Animation => EBoneRetargetingMode.Animation,
                    EBoneTranslationRetargetingMode.AnimationScaled => EBoneRetargetingMode.AnimationScaled,
                    EBoneTranslationRetargetingMode.AnimationRelative => EBoneRetargetingMode.AnimationRelative,
                    EBoneTranslationRetargetingMode.OrientAndScale => EBoneRetargetingMode.OrientAndScale,
                    _ => EBoneRetargetingMode.OrientAndScale //todo: other modes?
                };

                animSet.BoneModes[boneIndex] = boneMode;
            }

            // Check for NULL 'animSequence' only after CAnimSet is created: we're doing ConvertAnims(null) to create an empty AnimSet
            if (animSequence == null)
            {
                return animSet;
            }

            var numTracks = animSequence.GetNumTracks();

            // Store UAnimSequence in 'OriginalAnims' array, we just need it from time to time
            //OriginalAnims.Add(animSequence);

            // Create CAnimSequence
            var dst = new CAnimSequence(animSequence);
            animSet.Sequences.Add(dst);
            dst.Name = animSequence.Name;
            dst.NumFrames = animSequence.NumFrames;
            dst.Rate = animSequence.NumFrames / animSequence.SequenceLength * animSequence.RateScale;
            dst.bAdditive = animSequence.AdditiveAnimType != AAT_None;

            // Store information for animation retargeting.
            // Reference: UAnimSequence::GetRetargetTransforms()
            FTransform[]? retargetTransforms = null;
            if (animSequence.RetargetSource.IsNone && animSequence.RetargetSourceAssetReferencePose.Length > 0)
            {
                // We'll use RetargetSourceAssetReferencePose as a retarget base
                retargetTransforms = animSequence.RetargetSourceAssetReferencePose;
            }

            else
            {
                // Use USkeleton pose for retarget base.
                // Reference: USkeleton::GetRefLocalPoses()
                if (!animSequence.RetargetSource.IsNone)
                {
                    // The result might be NULL if there's no RetargetSource for this animation
                    if (skeleton.AnimRetargetSources.TryGetValue(animSequence.RetargetSource, out var refPose))
                    {
                        retargetTransforms = refPose.ReferencePose;
                    }
                }

                if (retargetTransforms == null)
                {
                    // Animation will use ReferenceSkeleton for retargeting, we've already copied the
                    // information into CAnimSet::BonePositions array
                }
            }

            if (retargetTransforms != null)
            {
                //todo: Solve this: RetargetTransforms size may not match ReferenceSkeleton and sequence's track count.
                //todo: UE4 does some remapping "track to skeleton bone index map". Without assertion things works, seems
                //todo: because RetargetTransforms array is smaller (or of the same size).
                //Trace.Assert(RetargetTransforms.Length == ReferenceSkeleton.FinalRefBoneInfo.Length);
                dst.RetargetBasePose = new CSkeletonBonePosition[retargetTransforms.Length];
                for (var i = 0; i < retargetTransforms.Length; i++)
                {
                    var boneTransform = retargetTransforms[i];
                    CSkeletonBonePosition bonePosition;
                    bonePosition.Position = boneTransform.Translation;
                    bonePosition.Orientation = boneTransform.Rotation;
                    dst.RetargetBasePose[i] = bonePosition;
                }
            }

            // bone tracks ...
            dst.Tracks = new List<CAnimTrack>(numTracks);

            if (animSequence.RawAnimationData is { Length: > 0 })
            {
                Trace.Assert(animSequence.RawAnimationData.Length == numTracks);

                static void CopyArray<T>(out T[] dest, T[] src)
                {
                    dest = new T[src.Length];
                    src.CopyTo(dest, 0);
                }

                for (var boneIndex = 0; boneIndex < numBones; boneIndex++)
                {
                    var track = new CAnimTrack();
                    dst.Tracks.Add(track);
                    var trackIndex = animSequence.FindTrackForBoneIndex(boneIndex);
                    if (trackIndex >= 0)
                    {
                        CopyArray(out track.KeyPos, animSequence.RawAnimationData[trackIndex].PosKeys);
                        CopyArray(out track.KeyQuat, animSequence.RawAnimationData[trackIndex].RotKeys);
                        var scaleKeys = animSequence.RawAnimationData[trackIndex].ScaleKeys;
                        if (scaleKeys != null)
                        {
                            CopyArray(out track.KeyScale, scaleKeys);
                        }
                        /*CopyArray(ref A.KeyTime, animSequence.RawAnimationData[TrackIndex].KeyTimes); // may be empty
                        for (int k = 0; k < A.KeyTime.Length; k++)
                            A.KeyTime[k] *= Dst.Rate;*/
                    }
                }
            }
            else if (animSequence.CompressedDataStructure is FUECompressedAnimData ueData)
            {
                // There could be an animation consisting of only trans with offsets == -1, what means
                // use of RefPose. In this case there's no point adding the animation to AnimSet. We'll
                // create FMemReader even for empty CompressedByteStream, otherwise it would be hard to
                // create a valid CAnimSequence which won't crash animation export.
                using var reader = new FByteArchive("CompressedByteStream", ueData.CompressedByteStream);
                for (var boneIndex = 0; boneIndex < numBones; boneIndex++)
                {
                    var track = new CAnimTrack();
                    dst.Tracks.Add(track);
                    var trackIndex = animSequence.FindTrackForBoneIndex(boneIndex);
                    if (trackIndex >= 0)
                    {
                        if (ueData.KeyEncodingFormat == AKF_PerTrackCompression)
                            ReadPerTrackData(reader, animSequence, track, trackIndex);
                        else
                            ReadKeyLerpData(reader, animSequence, track, trackIndex, ueData.KeyEncodingFormat == AKF_VariableKeyLerp);
                    }
                }
            }
            else if (animSequence.CompressedDataStructure is FACLCompressedAnimData aclData)
            {
                var tracks = aclData.GetCompressedTracks();
                var tracksHeader = tracks.GetTracksHeader();
                var numSamples = (int) tracksHeader.NumSamples;

                // Prepare buffers of all samples of each transform property for the native code to populate
                var translations = new FVector[numTracks * numSamples];
                var rotations = new FQuat[numTracks * numSamples];
                var scales = new FVector[numTracks * numSamples];

                // Let the native code do its job
                unsafe
                {
                    fixed (FVector* translationsPtr = translations)
                    fixed (FQuat* rotationsPtr = rotations)
                    fixed (FVector* scalesPtr = scales)
                    {
                        nReadACLData(tracks.Handle, translationsPtr, rotationsPtr, scalesPtr);
                    }
                }

                // Now create CAnimTracks with the data from those big buffers
                for (var boneIndex = 0; boneIndex < numBones; boneIndex++)
                {
                    var track = new CAnimTrack();
                    dst.Tracks.Add(track);
                    var trackIndex = animSequence.FindTrackForBoneIndex(boneIndex);
                    if (trackIndex >= 0)
                    {
                        var offset = trackIndex * numSamples;
                        track.KeyPos = new FVector[numSamples];
                        track.KeyQuat = new FQuat[numSamples];
                        track.KeyScale = new FVector[numSamples];
                        Array.Copy(translations, offset, track.KeyPos, 0, numSamples);
                        Array.Copy(rotations, offset, track.KeyQuat, 0, numSamples);
                        Array.Copy(scales, offset, track.KeyScale, 0, numSamples);
                    }
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException("Unsupported compressed data type " + animSequence.CompressedDataStructure.GetType().Name);
            }

            // Now should invert all imported rotations
            FixRotationKeys(dst);

            return animSet;
        }
    }
}