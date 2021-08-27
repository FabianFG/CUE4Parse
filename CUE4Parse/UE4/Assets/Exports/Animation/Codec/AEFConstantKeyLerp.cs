using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Assets.Exports.Animation.Codec
{
    internal class AEFConstantKeyLerp : AEFConstantKeyLerpShared
    {
        public AEFConstantKeyLerp(AnimationCompressionFormat format) : base(format) { }

        public override void GetBoneAtomRotation(FArchive Ar, FAnimSequenceDecompressionContext decompContext, int trackIndex, ref FTransform outAtom)
        {
            var animData = (FUECompressedAnimData) decompContext.CompressedAnimData;
            var trackData = animData.CompressedTrackOffsets;
            var trackDataStartIndex = trackIndex * 4;
            var rotKeysOffset = trackData[trackDataStartIndex + 2];
            var numRotKeys = trackData[trackDataStartIndex + 3];

            Ar.Position = rotKeysOffset;

            if (numRotKeys == 1)
            {
                AnimEncodingUtil.DecompressRotation(_format, Ar, rotKeysOffset, out var q);
                outAtom.Rotation = q;
            }
            else
            {
                var alpha = AnimEncodingUtil.TimeToIndex(
                    decompContext.SequenceLength,
                    decompContext.RelativePos,
                    numRotKeys,
                    decompContext.Interpolation,
                    out var index0,
                    out var index1);

                var rotStreamOffset = _format == AnimationCompressionFormat.ACF_IntervalFixed32NoW ? sizeof(float) * 6 : 0; // offset past Min and Range data
                if (index0 != index1)
                {
                    var keyData0Offset = rotKeysOffset + rotStreamOffset + index0 * AnimEncodingUtil.GetCompressedRotationStride(_format) * AnimEncodingUtil.GetCompressedRotationNum(_format);
                    var keyData1Offset = rotKeysOffset + rotStreamOffset + index1 * AnimEncodingUtil.GetCompressedRotationStride(_format) * AnimEncodingUtil.GetCompressedRotationNum(_format);
                    AnimEncodingUtil.DecompressRotation(_format, Ar, keyData0Offset, out var q1);
                    AnimEncodingUtil.DecompressRotation(_format, Ar, keyData1Offset, out var q2);
                    var rot = FQuat.FastLerp(q1, q2, alpha);
                    rot.Normalize();
                    outAtom.Rotation = rot;
                }
                else
                {
                    var keyDataOffset = rotKeysOffset + rotStreamOffset + index0 * AnimEncodingUtil.GetCompressedRotationStride(_format) * AnimEncodingUtil.GetCompressedRotationNum(_format);
                    AnimEncodingUtil.DecompressRotation(_format, Ar, keyDataOffset, out var q);
                    outAtom.Rotation = q;
                }
            }
        }

        public override void GetBoneAtomTranslation(FArchive Ar, FAnimSequenceDecompressionContext decompContext, int trackIndex, ref FTransform outAtom)
        {
            var animData = (FUECompressedAnimData) decompContext.CompressedAnimData;

            var trackData = animData.CompressedTrackOffsets;
            var trackDataStartIndex = trackIndex * 4;
            var transKeysOffset = trackData[trackDataStartIndex];
            var numTransKeys = trackData[trackDataStartIndex + 1];

            var alpha = AnimEncodingUtil.TimeToIndex(
                decompContext.SequenceLength,
                decompContext.RelativePos,
                numTransKeys,
                decompContext.Interpolation,
                out var index0,
                out var index1);

            Ar.Position = transKeysOffset;
            var transStreamOffset = ((_format == AnimationCompressionFormat.ACF_IntervalFixed32NoW) && numTransKeys > 1) ? sizeof(float) * 6 : 0; // offset past Min and Range data

            if (index0 != index1)
            {
                var keyData0Offset = transKeysOffset + transStreamOffset + index0 * AnimEncodingUtil.GetCompressedTranslationStride(_format) * AnimEncodingUtil.GetCompressedTranslationNum(_format);
                var keyData1Offset = transKeysOffset + transStreamOffset + index1 * AnimEncodingUtil.GetCompressedTranslationStride(_format) * AnimEncodingUtil.GetCompressedTranslationNum(_format);
                AnimEncodingUtil.DecompressTranslation(_format, Ar, keyData0Offset, out var v1);
                AnimEncodingUtil.DecompressTranslation(_format, Ar, keyData1Offset, out var v2);
                outAtom.Translation = MathUtils.Lerp(v1, v2, alpha);
            }
            else
            {
                var keyDataOffset = transKeysOffset + transStreamOffset + index0 * AnimEncodingUtil.GetCompressedTranslationStride(_format) * AnimEncodingUtil.GetCompressedTranslationNum(_format);
                AnimEncodingUtil.DecompressTranslation(_format, Ar, keyDataOffset, out var v);
                outAtom.Translation = v;
            }
        }

        public override void GetBoneAtomScale(FArchive Ar, FAnimSequenceDecompressionContext decompContext, int trackIndex, ref FTransform outAtom)
        {
            var animData = (FUECompressedAnimData) decompContext.CompressedAnimData;

            var scaleKeysOffset = animData.CompressedScaleOffsets.GetOffsetData(trackIndex, 0);
            var numScaleKeys = animData.CompressedScaleOffsets.GetOffsetData(trackIndex, 1);
            Ar.Position = scaleKeysOffset;

            var alpha = AnimEncodingUtil.TimeToIndex(
                decompContext.SequenceLength,
                decompContext.RelativePos,
                numScaleKeys,
                decompContext.Interpolation,
                out var index0,
                out var index1);

            var scaleStreamOffset = ((_format == AnimationCompressionFormat.ACF_IntervalFixed32NoW) && numScaleKeys > 1) ? sizeof(float) * 6 : 0; // offset past Min and Range data

            if (index0 != index1)
            {
                var keyData0Offset = scaleKeysOffset + scaleStreamOffset + index0 * AnimEncodingUtil.GetCompressedScaleStride(_format) * AnimEncodingUtil.GetCompressedScaleNum(_format);
                var keyData1Offset = scaleKeysOffset + scaleStreamOffset + index1 * AnimEncodingUtil.GetCompressedScaleStride(_format) * AnimEncodingUtil.GetCompressedScaleNum(_format);
                AnimEncodingUtil.DecompressScale(_format, Ar, keyData0Offset, out var v1);
                AnimEncodingUtil.DecompressScale(_format, Ar, keyData1Offset, out var v2);
                outAtom.Scale3D = MathUtils.Lerp(v1, v2, alpha);
            }
            else
            {
                var keyDataOffset = scaleKeysOffset + scaleStreamOffset + index0 * AnimEncodingUtil.GetCompressedTranslationStride(_format) * AnimEncodingUtil.GetCompressedTranslationNum(_format);
                AnimEncodingUtil.DecompressScale(_format, Ar, keyDataOffset, out var v);
                outAtom.Translation = v;
            }
        }
    }
}