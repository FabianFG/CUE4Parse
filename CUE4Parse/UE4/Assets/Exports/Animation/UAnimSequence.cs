using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Serilog;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public class UAnimSequence : UAnimationAsset
    {
        public int NumFrames;

        // UAnimSequenceBase
        public float SequenceLength;
        public float RateScale;
        public EAdditiveAnimationType AdditiveAnimType;
        public FName RetargetSource;
        public FTransform[] RetargetSourceAssetReferencePose; 

        public FRawAnimSequenceTrack[] RawAnimationData;
        public byte[] CompressedByteStream;
        public FCompressedSegment[] CompressedSegments;
        public bool bUseRawDataOnly;

        public AnimationKeyFormat KeyEncodingFormat;
        public AnimationCompressionFormat TranslationCompressionFormat;
        public AnimationCompressionFormat RotationCompressionFormat;
        public AnimationCompressionFormat ScaleCompressionFormat;
        public int[] CompressedTrackOffsets;
        public FCompressedOffsetData CompressedScaleOffsets;
        public FTrackToSkeletonMap[] TrackToSkeletonMapTable; // used for raw data
        public FTrackToSkeletonMap[] CompressedTrackToSkeletonMapTable; // used for compressed data, missing before 4.12
        public FStructFallback CompressedCurveData; // FRawCurveTracks

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            var stripFlags = new FStripDataFlags(Ar);
            if (!stripFlags.IsEditorDataStripped())
            {
                RawAnimationData = Ar.ReadArray(() => new FRawAnimSequenceTrack(Ar));
                if (Ar.Ver >= UE4Version.VER_UE4_ANIMATION_ADD_TRACKCURVES)
                {
                    //if (FUE5MainStreamObjectVersion.Get(Ar) < FUE5MainStreamObjectVersion.Type.RemovingSourceAnimationData)
                    //{
                    var sourceRawAnimationData = Ar.ReadArray(() => new FRawAnimSequenceTrack(Ar));
                    //}
                }
            }

            if (FFrameworkObjectVersion.Get(Ar) < FFrameworkObjectVersion.Type.MoveCompressedAnimDataToTheDDC)
            {
                /*// Part of data were serialized as properties
                CompressedByteStream = Ar.ReadArray<byte>();
                if (Ar.Game == EGame.GAME_SEAOFTHIEVES && CompressedByteStream.Num() == 1 && Ar.Length - Ar.Position > 0)
                {
                    // Sea of Thieves has extra int32 == 1 before the CompressedByteStream
                    Ar.Position -= 1;
                    CompressedByteStream = Ar.ReadArray<byte>();
                }

                // Fix layout of "byte swapped" data (workaround for UE4 bug)
                if (KeyEncodingFormat == AnimationKeyFormat.AKF_PerTrackCompression && CompressedScaleOffsets.OffsetData.Length > 0)
                {
                    TArray<uint8> SwappedData;
                    TransferPerTrackData(SwappedData, CompressedByteStream);
                    Exchange(SwappedData, CompressedByteStream);
                }*/
                throw new NotImplementedException();
            }
            else
            {
                // UE4.12+
                var bSerializeCompressedData = Ar.ReadBoolean();

                if (bSerializeCompressedData)
                {
                    if (Ar.Game < EGame.GAME_UE4_23)
                        SerializeCompressedData(Ar);
                    else if (Ar.Game < EGame.GAME_UE4_25)
                        SerializeCompressedData2(Ar);
                    else
                        SerializeCompressedData3(Ar);

                    bUseRawDataOnly = Ar.ReadBoolean();
                }
            }
        }

        private void SerializeCompressedData(FAssetArchive Ar)
        {
            // These fields were serialized as properties in pre-UE4.12 engine version
            KeyEncodingFormat = Ar.Read<AnimationKeyFormat>();
            TranslationCompressionFormat = Ar.Read<AnimationCompressionFormat>();
            RotationCompressionFormat = Ar.Read<AnimationCompressionFormat>();
            ScaleCompressionFormat = Ar.Read<AnimationCompressionFormat>();

            CompressedTrackOffsets = Ar.ReadArray<int>();
            CompressedScaleOffsets = new FCompressedOffsetData(Ar);

            if (Ar.Game >= EGame.GAME_UE4_21)
            {
                // UE4.21+ - added compressed segments; disappeared in 4.23
                CompressedSegments = Ar.ReadArray<FCompressedSegment>();
                if (CompressedSegments.Length > 0)
                {
                    Log.Information("animation has CompressedSegments!");
                }
            }

            CompressedTrackToSkeletonMapTable = Ar.ReadArray<FTrackToSkeletonMap>();

            if (Ar.Game < EGame.GAME_UE4_22)
            {
                CompressedCurveData = new FStructFallback(Ar, "RawCurveTracks");
            }
            else
            {
                var compressedCurveNames = Ar.ReadArray(() => new FSmartName(Ar));
            }

            if (Ar.Game >= EGame.GAME_UE4_17)
            {
                // UE4.17+
                var compressedRawDataSize = Ar.Read<int>();
            }

            if (Ar.Game >= EGame.GAME_UE4_22)
            {
                var compressedNumFrames = Ar.Read<int>();
            }

            // compressed data
            var numBytes = Ar.Read<int>();
            CompressedByteStream = Ar.ReadBytes(numBytes);

            if (Ar.Game >= EGame.GAME_UE4_22)
            {
                var curveCodecPath = Ar.ReadFString();
                var compressedCurveByteStream = Ar.ReadArray<byte>();
            }

            // Fix layout of "byte swapped" data (workaround for UE4 bug)
            if (KeyEncodingFormat == AnimationKeyFormat.AKF_PerTrackCompression && CompressedScaleOffsets.OffsetData.Length > 0 && Ar.Game < EGame.GAME_UE4_23)
            {
                throw new NotImplementedException();
            }
        }

        // UE4.23-4.24 has changed compressed data layout for streaming, so it's worth making a separate
        // serializer function for it.
        private void SerializeCompressedData2(FAssetArchive Ar)
        {
            var compressedRawDataSize = Ar.Read<int>();
            CompressedTrackToSkeletonMapTable = Ar.ReadArray<FTrackToSkeletonMap>();
            var compressedCurveNames = Ar.ReadArray(() => new FSmartName(Ar));

            // Since 4.23, this is FUECompressedAnimData::SerializeCompressedData
            KeyEncodingFormat = Ar.Read<AnimationKeyFormat>();
            TranslationCompressionFormat = Ar.Read<AnimationCompressionFormat>();
            RotationCompressionFormat = Ar.Read<AnimationCompressionFormat>();
            ScaleCompressionFormat = Ar.Read<AnimationCompressionFormat>();

            var compressedNumFrames = Ar.Read<int>();

            // SerializeView() just serializes array size
            var compressedTrackOffsetsNum = Ar.Read<int>();
            var compressedScaleOffsetsNum = Ar.Read<int>();
            CompressedScaleOffsets = new FCompressedOffsetData(Ar.Read<int>());
            var compressedByteStreamNum = Ar.Read<int>();
            // ... end of FUECompressedAnimData::SerializeCompressedData

            var numBytes = Ar.Read<int>();
            var bUseBulkDataForLoad = Ar.ReadBoolean();

            // In UE4.23 CompressedByteStream field exists in FUECompressedAnimData (as TArrayView) and in
            // FCompressedAnimSequence (as byte array). Serialization is done in FCompressedAnimSequence,
            // either as TArray or as bulk, and then array is separated onto multiple "views" for
            // FUECompressedAnimData. We'll use a different name for "joined" serialized array here to
            // avoid confuse.
            byte[] serializedByteStream;

            if (bUseBulkDataForLoad)
            {
                throw new NotImplementedException("Anim: bUseBulkDataForLoad not implemented");
                //todo: read from bulk to serializedByteStream
            }
            else
            {
                serializedByteStream = Ar.ReadBytes(numBytes);
            }

            // Setup all array views from single array. In UE4 this is done in FUECompressedAnimData::InitViewsFromBuffer.
            // We'll simply copy array data away from SerializedByteStream, and then SerializedByteStream
            // will be released from memory as it is a local variable here.
            // Note: copying is not byte-order wise, so if there will be any problems in the future,
            // should use byte swap functions.
            using (var tempAr = new FByteArchive("SerializedByteStream", serializedByteStream, Ar.Versions))
            {
                CompressedTrackOffsets = tempAr.ReadArray<int>(compressedTrackOffsetsNum);
                CompressedScaleOffsets.OffsetData = tempAr.ReadArray<int>(compressedScaleOffsetsNum);
                CompressedByteStream = tempAr.ReadBytes(compressedByteStreamNum);
            }

            var curveCodecPath = Ar.ReadFString();
            var compressedCurveByteStream = Ar.ReadArray<byte>();
        }

        // UE4.25 has changed data layout, and therefore serialization order has been changed too.
        // In UE4.25 serialization is done in FCompressedAnimSequence::SerializeCompressedData().
        private void SerializeCompressedData3(FAssetArchive Ar)
        {
            var compressedRawDataSize = Ar.Read<int>();
            CompressedTrackToSkeletonMapTable = Ar.ReadArray<FTrackToSkeletonMap>();
            var compressedCurveNames = Ar.ReadArray(() => new FSmartName(Ar));

            var numBytes = Ar.Read<int>();
            var bUseBulkDataForLoad = Ar.ReadBoolean();

            // In UE4.23 CompressedByteStream field exists in FUECompressedAnimData (as TArrayView) and in
            // FCompressedAnimSequence (as byte array). Serialization is done in FCompressedAnimSequence,
            // either as TArray or as bulk, and then array is separated onto multiple "views" for
            // FUECompressedAnimData. We'll use a different name for "joined" serialized array here to
            // avoid confuse.
            byte[] serializedByteStream;

            if (bUseBulkDataForLoad)
            {
                throw new NotImplementedException("Anim: bUseBulkDataForLoad not implemented");
                //todo: read from bulk to serializedByteStream
            }
            else
            {
                serializedByteStream = Ar.ReadBytes(numBytes);
            }

            var boneCodecDDCHandle = Ar.ReadFString();
            var curveCodecPath = Ar.ReadFString();

            var numCurveBytes = Ar.Read<int>();
            var compressedCurveByteStream = Ar.ReadBytes(numCurveBytes);

            if (boneCodecDDCHandle.Length > 0)
            {
                // The following part is ICompressedAnimData::SerializeCompressedData
                var compressedNumFrames = Ar.Read<int>();
                // todo: editor-only data here

                // FUECompressedAnimData::SerializeCompressedData
                KeyEncodingFormat = Ar.Read<AnimationKeyFormat>();
                TranslationCompressionFormat = Ar.Read<AnimationCompressionFormat>();
                RotationCompressionFormat = Ar.Read<AnimationCompressionFormat>();
                ScaleCompressionFormat = Ar.Read<AnimationCompressionFormat>();

                // SerializeView() just serializes array size
                var compressedByteStreamNum = Ar.Read<int>();
                var compressedTrackOffsetsNum = Ar.Read<int>();
                var compressedScaleOffsetsNum = Ar.Read<int>();
                CompressedScaleOffsets = new FCompressedOffsetData(Ar.Read<int>());

                // Setup all array views from single array. In UE4 this is done in FUECompressedAnimData::InitViewsFromBuffer.
                // We'll simply copy array data away from SerializedByteStream, and then SerializedByteStream
                // will be released from memory as it is a local variable here.
                // Note: copying is not byte-order wise, so if there will be any problems in the future,
                // should use byte swap functions.
                using (var tempAr = new FByteArchive("SerializedByteStream", serializedByteStream, Ar.Versions))
                {
                    CompressedTrackOffsets = tempAr.ReadArray<int>(compressedTrackOffsetsNum);
                    CompressedScaleOffsets.OffsetData = tempAr.ReadArray<int>(compressedScaleOffsetsNum);
                    CompressedByteStream = tempAr.ReadBytes(compressedByteStreamNum);
                }
            }
        }

        // WARNING: the following functions uses some logic to use either CompressedTrackToSkeletonMapTable or TrackToSkeletonMapTable.
        // This logic should be the same everywhere. Note: CompressedTrackToSkeletonMapTable appeared in UE4.12, so it will always be
        // empty when loading animations from older engines.

        public int GetNumTracks() => CompressedTrackToSkeletonMapTable.Length > 0 ?
            CompressedTrackToSkeletonMapTable.Length :
            TrackToSkeletonMapTable.Length;

        public int GetTrackBoneIndex(int TrackIndex) => CompressedTrackToSkeletonMapTable.Length > 0 ?
            CompressedTrackToSkeletonMapTable[TrackIndex].BoneTreeIndex :
            TrackToSkeletonMapTable[TrackIndex].BoneTreeIndex;

        public int FindTrackForBoneIndex(int BoneIndex) {
            var TrackMap = CompressedTrackToSkeletonMapTable.Length > 0 ? CompressedTrackToSkeletonMapTable : TrackToSkeletonMapTable;
            for (int TrackIndex = 0; TrackIndex < TrackMap.Length; TrackIndex++)
            {
                if (TrackMap[TrackIndex].BoneTreeIndex == BoneIndex)
                    return TrackIndex;
            }
            return -1;
        }
    }
}