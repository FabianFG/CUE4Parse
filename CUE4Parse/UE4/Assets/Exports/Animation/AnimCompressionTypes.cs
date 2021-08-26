using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;
using static CUE4Parse.UE4.Assets.Exports.Animation.AnimationCompressionFormat;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    using System;
    using System.Diagnostics;

    using CUE4Parse.UE4.Assets.Exports.Animation.Codec;

    public class FCompressedOffsetDataBase<T> where T : struct
    {
        public T[] OffsetData;
        public int OffsetDataNum;
        public int StripSize;

        public FCompressedOffsetDataBase(int stripSize = 2)
        {
            StripSize = stripSize;
        }

        public FCompressedOffsetDataBase(FArchive Ar)
        {
            OffsetData = Ar.ReadArray<T>();
            StripSize = Ar.Read<int>();
        }

        public T GetOffsetData(int stripIndex, int offset)
        {
            return OffsetData[stripIndex * StripSize + offset];
        }

        public bool IsValid() => StripSize > 0 && OffsetData.Length > 0;
    }

    public class FCompressedOffsetData : FCompressedOffsetDataBase<int>
    {
        public FCompressedOffsetData(int stripSize = 2) : base(stripSize) { }
        public FCompressedOffsetData(FArchive Ar) : base(Ar) { }
    }

    public class FCompressedAnimDataBase
    {
        /**
         * An array of 4*NumTrack ints, arranged as follows: - PerTrack is 2*NumTrack, so this isn't true any more
         *   [0] Trans0.Offset
         *   [1] Trans0.NumKeys
         *   [2] Rot0.Offset
         *   [3] Rot0.NumKeys
         *   [4] Trans1.Offset
         *   . . .
         */
        public int[] CompressedTrackOffsets;

        /**
        * An array of 2*NumTrack ints, arranged as follows:
        *  if identity, it is offset
        *  if not, it is num of keys
        *   [0] Scale0.Offset or NumKeys
        *   [1] Scale1.Offset or NumKeys
        * @TODO NOTE: first implementation is offset is [0], numkeys [1]
        *   . . .
        */
        public FCompressedOffsetData CompressedScaleOffsets = new();

        public byte[] CompressedByteStream;

        public IAnimEncoding TranslationCodec;
        public IAnimEncoding RotationCodec;
        public IAnimEncoding ScaleCodec;

        public AnimationKeyFormat KeyEncodingFormat;

        // The compression format that was used to compress tracks parts.
        public AnimationCompressionFormat TranslationCompressionFormat;
        public AnimationCompressionFormat RotationCompressionFormat;
        public AnimationCompressionFormat ScaleCompressionFormat;

        


}

    public interface ICompressedAnimData
    {
        /* Common data */
        int CompressedNumberOfFrames { get; set; } // CompressedNumberOfKeys in UE5

        //public FAnimationErrorStats BoneCompressionErrorStats; //editor

        void SerializeCompressedDataBase(FAssetArchive Ar)
        {
            FCompressedAnimDataBase.BaseSerializeCompressedData(this, Ar);
        }

        void Bind(byte[] bulkData);

        string GetDebugString();
    }

    public class FUECompressedAnimData : FCompressedAnimDataBase, ICompressedAnimData
    {
        public int CompressedNumberOfFrames { get; set; }

        public void InitViewsFromBuffer(byte[] bulkData)
        {
            using var tempAr = new FByteArchive("SerializedByteStream", bulkData);
            CompressedTrackOffsets = tempAr.ReadArray<int>(CompressedTrackOffsets.Length);
            CompressedScaleOffsets.OffsetData = tempAr.ReadArray<int>(CompressedScaleOffsets.OffsetDataNum);
            CompressedByteStream = tempAr.ReadBytes(CompressedByteStream.Length);
        }

        public void SerializeCompressedData(FAssetArchive ar)
        {
            ((ICompressedAnimData)this).SerializeCompressedDataBase(ar);
            KeyEncodingFormat = ar.Read<AnimationKeyFormat>();
            TranslationCompressionFormat = ar.Read<AnimationCompressionFormat>();
            RotationCompressionFormat = ar.Read<AnimationCompressionFormat>();
            ScaleCompressionFormat = ar.Read<AnimationCompressionFormat>();
            CompressedByteStream = new byte[ar.Read<int>()];
            CompressedTrackOffsets = new int[ar.Read<int>()];
            CompressedScaleOffsets.OffsetData = new int[ar.Read<int>()];
            SetInterfaceLinks();
        }

        private void SetInterfaceLinks()
        {
            if (KeyEncodingFormat == AnimationKeyFormat.AKF_ConstantKeyLerp)
            {
                // setup translation codec
                TranslationCodec = TranslationCompressionFormat switch
                {
                    ACF_None or ACF_Float96NoW or ACF_IntervalFixed32NoW or ACF_Identity => new AEFConstantKeyLerp(TranslationCompressionFormat),
                    _ => throw new ArgumentOutOfRangeException(
                             $"{ TranslationCompressionFormat }: unknown or unsupported translation compression")
                };

                // setup rotation codec
                RotationCodec = RotationCompressionFormat switch
                {
                    ACF_None or ACF_Float96NoW or ACF_Fixed48NoW or ACF_IntervalFixed32NoW or ACF_Fixed32NoW or ACF_Float32NoW or ACF_Identity => new AEFConstantKeyLerp(RotationCompressionFormat),
                    _ => throw new ArgumentOutOfRangeException(
                             $"{ RotationCompressionFormat }: unknown or unsupported rotation compression")
                };

                // setup scale codec
                ScaleCodec = ScaleCompressionFormat switch
                {
                    ACF_None or ACF_Float96NoW or ACF_IntervalFixed32NoW or ACF_Identity => new AEFConstantKeyLerp(ScaleCompressionFormat),
                    _ => throw new ArgumentOutOfRangeException(
                             $"{ ScaleCompressionFormat }: unknown or unsupported scale compression")
                };
            }
            else if (KeyEncodingFormat == AnimationKeyFormat.AKF_VariableKeyLerp)
            {

            }
            else if (KeyEncodingFormat == AnimationKeyFormat.AKF_PerTrackCompression)
            {
                TranslationCodec = RotationCodec = ScaleCodec = new AEFPerTrackCompressionCodec();
                Debug.Assert(RotationCompressionFormat == ACF_Identity, "RotationCompressionFormat == ACF_Identity");
                Debug.Assert(TranslationCompressionFormat == ACF_Identity, "TranslationCompressionFormat == ACF_Identity");
            }
            else
            {
                throw new ArgumentOutOfRangeException($"{KeyEncodingFormat}: unknown or unsupported animation format");
            }
        }

        public void Bind(byte[] bulkData) => InitViewsFromBuffer(bulkData);

        public string GetDebugString() => $"[{TranslationCompressionFormat}, {RotationCompressionFormat}, {ScaleCompressionFormat}]";
    }
}