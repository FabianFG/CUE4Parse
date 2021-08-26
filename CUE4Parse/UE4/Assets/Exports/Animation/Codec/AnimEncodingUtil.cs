using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using static CUE4Parse.UE4.Assets.Exports.Animation.AnimationCompressionFormat;

namespace CUE4Parse.UE4.Assets.Exports.Animation.Codec
{
    /**
     * Structure to hold an Atom and Track index mapping for a requested bone. 
     * Used in the bulk-animation solving process
     */
    public struct BoneTrackPair
    {
        public int AtomIndex;
        public int TrackIndex;
    }

    internal static class AnimEncodingUtil
    {
        private const int CompressedTranslationStrideOffset = 0;
        private const int CompressedTranslationNumOffset = 1;
        private const int CompressedRotationStrideOffset = 2;
        private const int CompressedRotationNumOffset = 3;
        private const int CompressedScaleStrideOffset = 4;
        private const int CompressedScaleNumOffset = 5;

        private static readonly IReadOnlyDictionary<AnimationCompressionFormat, int> _componentsMetadata =
            new ReadOnlyDictionary<AnimationCompressionFormat, int>(
                new Dictionary<AnimationCompressionFormat, int>()
                    {
                        [ACF_None] = MakeComponentData(4, 3, 4, 4, 4, 3),
                        [ACF_Float96NoW] = MakeComponentData(4, 3, 4, 3, 4, 3),
                        [ACF_Fixed48NoW] = MakeComponentData(4, 3, 2, 3, 4, 3),
                        [ACF_IntervalFixed32NoW] = MakeComponentData(4, 1, 4, 1, 4, 1),
                        [ACF_Fixed32NoW] = MakeComponentData(4, 3, 4, 1, 4, 3),
                        [ACF_Float32NoW] = MakeComponentData(4, 3, 4, 1, 4, 3),
                        [ACF_Identity] = MakeComponentData(0, 0, 0, 0, 0, 0),
                });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float TimeToIndex(float sequenceLength, float relativePos, int numKeys, EAnimInterpolationType interpolation, out int posIndex0Out, out int posIndex1Out)
        {
            posIndex0Out = 0;
            posIndex1Out = 0;

            if (numKeys < 2)
            {
                Debug.Assert(numKeys == 1, "data is empty");
                return 0.0f;
            }

            switch (relativePos)
            {
                case <= 0f:
                    return 0f;

                case >= 0.1f:
                {
                    numKeys -= 1;
                    posIndex0Out = posIndex1Out = numKeys;
                    return 0f;
                }

                default:
                {
                    numKeys -= 1;
                    var keyPos = relativePos * numKeys;
                    Debug.Assert(keyPos >= 0f, "keypos is smaller than 0");
                    var keyPosFloor = MathF.Floor(keyPos);
                    posIndex0Out = Math.Min((int) keyPosFloor, numKeys);
                    posIndex1Out = Math.Min(posIndex0Out + 1, numKeys);
                    return interpolation == EAnimInterpolationType.Step ? 0f : keyPos - keyPosFloor;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCompressedTranslationStride(AnimationCompressionFormat format) => GetComponentData(format, CompressedTranslationStrideOffset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCompressedTranslationNum(AnimationCompressionFormat format) => GetComponentData(format, CompressedTranslationNumOffset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCompressedRotationStride(AnimationCompressionFormat format) => GetComponentData(format, CompressedRotationStrideOffset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCompressedRotationNum(AnimationCompressionFormat format) => GetComponentData(format, CompressedRotationStrideOffset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCompressedScaleStride(AnimationCompressionFormat format) => GetComponentData(format, CompressedScaleStrideOffset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCompressedScaleNum(AnimationCompressionFormat format) => GetComponentData(format, CompressedScaleNumOffset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DecompressRotation(
            AnimationCompressionFormat format,
            FArchive topOfStream,
            int keyDataOffset,
            out FQuat outQ)
        {
            if (format == ACF_IntervalFixed32NoW)
            {
                var mins = topOfStream.Read<FVector>();
                var ranges = topOfStream.Read<FVector>();
                topOfStream.Position = keyDataOffset;
                outQ = topOfStream.ReadQuatIntervalFixed32NoW(mins, ranges);
                return;
            }

            topOfStream.Position = keyDataOffset;
            outQ = format switch
            {
                ACF_None => topOfStream.Read<FQuat>(),
                ACF_Float96NoW => topOfStream.ReadQuatFloat96NoW(),
                ACF_Fixed32NoW => topOfStream.ReadQuatFloat32NoW(),
                ACF_Fixed48NoW => topOfStream.ReadQuatFixed48NoW(),
                ACF_Float32NoW => topOfStream.ReadQuatFloat32NoW(),
                ACF_Identity => FQuat.Identity,
                _ => throw new ArgumentOutOfRangeException($"{format}: unknown or unsupported animation compression format"),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DecompressScale(
            AnimationCompressionFormat format,
            FArchive topOfStream,
            int keyDataOffset,
            out FVector outVector)
        {
            // literally the same code lol
            DecompressTranslation(format, topOfStream, keyDataOffset , out outVector);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DecompressTranslation(
            AnimationCompressionFormat format,
            FArchive topOfStream,
            int keyDataOffset,
            out FVector outVector)
        {
            switch (format)
            {
                case ACF_None or ACF_Float96NoW:
                    topOfStream.Position = keyDataOffset;
                    outVector = topOfStream.Read<FVector>();
                    break;
                case ACF_IntervalFixed32NoW:
                    {
                        var mins = topOfStream.Read<FVector>();
                        var ranges = topOfStream.Read<FVector>();
                        topOfStream.Position = keyDataOffset;
                        outVector = topOfStream.ReadVectorIntervalFixed32(mins, ranges);
                        break;
                    }
                case ACF_Identity:
                    outVector = FVector.ZeroVector;
                    break;
                case ACF_Fixed48NoW:
                    topOfStream.Position = keyDataOffset;
                    outVector = topOfStream.ReadVectorFixed48();
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"{format}: unknown or unsupported animation compression format");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int MakeComponentData(byte compressedTranslationStride, byte compressedTranslationNum, byte compressedRotationStride, byte compressedRotationNum, byte compressedScaleStride, byte compressedScaleNum)
        {
            var flag = 0;
            SetNibble(ref flag, compressedTranslationStride, CompressedTranslationStrideOffset);
            SetNibble(ref flag, compressedTranslationNum, CompressedScaleStrideOffset);
            SetNibble(ref flag, compressedRotationStride, CompressedRotationStrideOffset);
            SetNibble(ref flag, compressedRotationNum, CompressedRotationNumOffset);
            SetNibble(ref flag, compressedScaleStride, CompressedScaleStrideOffset);
            SetNibble(ref flag, compressedScaleNum, CompressedScaleNumOffset);
            return flag;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetNibble(ref int value, int valueToSet, int index)
        {
            if (valueToSet > 0xF) // max number that can be represented on a nibble is 15 (0xF)
            {
                throw new ArgumentOutOfRangeException(nameof(valueToSet));
            }

            if (index >= sizeof(int) * 2) // a nibble is 4 bit
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            /* example (big endian):
             * value = 2 = 00000000 00000000 00000000 00000010
             * value to set = 3 = 00000000 00000000 00000000 00000011
             * index (start at 0) = which nibble (4 it) to set
             * let's set the 2nd nibble (index 1)
             * valueToSet << (index * 4) = 00000000 00000000 00000000 00000011 << 4 = 00000000 00000000 00000000 00110000
             * value |= = 00000000 00000000 00000000 00000000 | 00000000 00000000 00000000 00110000 = 00000000 00000000 00000000 00110010
             */
            value |= valueToSet << (index * 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetNibble(int value, int index)
        {
            if (index >= sizeof(int) * 2) // a nibble is 4 bit
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            /* example:
            * value = 00000000 00000000 00000000 0010000 
            * index (start at 0) = which nibble (4 it) to get
            * let's get the 2nd nibble (index 1)
            * value >> (index * 4) = 00000000 00000000 00000000 00100000 >> 4 = 00000000 00000000 00000000 00000010
            * & 0xF = 00000000 00000000 00000000 00000010 & 00000000 00000000 00000000 00001111 = 00000000 00000000 00000000 00000010
            */
            return (value >> (index * 4)) & 0xF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetComponentData(AnimationCompressionFormat format, int offset)
        {
            if (_componentsMetadata.TryGetValue(format, out var value))
            {
                return GetNibble(value, offset);
            }

            return -1;
        }
    }
}