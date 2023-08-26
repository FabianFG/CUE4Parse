using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public class FCompressedOffsetDataBase<T> where T : struct
    {
        public T[] OffsetData = Array.Empty<T>();
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

        public AnimationKeyFormat KeyEncodingFormat;

        // The compression format that was used to compress tracks parts.
        public AnimationCompressionFormat TranslationCompressionFormat;
        public AnimationCompressionFormat RotationCompressionFormat;
        public AnimationCompressionFormat ScaleCompressionFormat;
    }

    public interface ICompressedAnimData
    {
        /* Common data */
        public int CompressedNumberOfFrames { get; set; } // CompressedNumberOfKeys in UE5
        //public FAnimationErrorStats BoneCompressionErrorStats; //editor

        public void SerializeCompressedData(FAssetArchive Ar)
        {
            BaseSerializeCompressedData(Ar);
        }

        internal void BaseSerializeCompressedData(FAssetArchive Ar)
        {
            CompressedNumberOfFrames = Ar.Read<int>();
            /*if (!Ar.Owner.HasFlags(EPackageFlags.PKG_FilterEditorOnly))
            {
                BoneCompressionErrorStats = new FAnimationErrorStats(Ar);
            }*/
        }

        public void Bind(byte[] bulkData);
    }

    [JsonConverter(typeof(FUECompressedAnimDataConverter))]
    public class FUECompressedAnimData : FCompressedAnimDataBase, ICompressedAnimData
    {
        public int CompressedNumberOfFrames { get; set; }

        public void InitViewsFromBuffer(byte[] bulkData)
        {
            using var tempAr = new FByteArchive("SerializedByteStream", bulkData);
            tempAr.ReadArray(CompressedTrackOffsets);
            tempAr.ReadArray(CompressedScaleOffsets.OffsetData);
            tempAr.ReadArray(CompressedByteStream);
        }

        public void SerializeCompressedData(FAssetArchive Ar)
        {
            var baseFirst = Ar.Game >= EGame.GAME_UE4_25;

            if (baseFirst)
            {
                ((ICompressedAnimData) this).BaseSerializeCompressedData(Ar);
            }

            KeyEncodingFormat = Ar.Read<AnimationKeyFormat>();
            TranslationCompressionFormat = Ar.Read<AnimationCompressionFormat>();
            RotationCompressionFormat = Ar.Read<AnimationCompressionFormat>();
            ScaleCompressionFormat = Ar.Read<AnimationCompressionFormat>();

            if (!baseFirst)
            {
                ((ICompressedAnimData) this).BaseSerializeCompressedData(Ar);
            }

            if (baseFirst)
            {
                CompressedByteStream = new byte[Ar.Read<int>()];
            }

            CompressedTrackOffsets = new int[Ar.Read<int>()];
            CompressedScaleOffsets.OffsetData = new int[Ar.Read<int>()];
            CompressedScaleOffsets.StripSize = Ar.Read<int>();

            if (!baseFirst)
            {
                CompressedByteStream = new byte[Ar.Read<int>()];
            }
        }

        public void Bind(byte[] bulkData) => InitViewsFromBuffer(bulkData);

        public override string ToString() => $"[{TranslationCompressionFormat}, {RotationCompressionFormat}, {ScaleCompressionFormat}]";
    }
}
