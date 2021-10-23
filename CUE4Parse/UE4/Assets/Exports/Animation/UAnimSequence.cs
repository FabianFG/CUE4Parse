using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Serilog;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public class UAnimSequence : UAnimationAsset
    {
        public int NumFrames;
        public FTrackToSkeletonMap[] TrackToSkeletonMapTable; // used for raw data
        public FRawAnimSequenceTrack[] RawAnimationData;
        public ResolvedObject? BoneCompressionSettings; // UAnimBoneCompressionSettings
        // begin CompressedData
        public FTrackToSkeletonMap[] CompressedTrackToSkeletonMapTable; // used for compressed data, missing before 4.12
        public FStructFallback CompressedCurveData; // FRawCurveTracks
        public ICompressedAnimData CompressedDataStructure;
        public UAnimBoneCompressionCodec? BoneCompressionCodec;
        // end CompressedData
        public EAdditiveAnimationType AdditiveAnimType;
        public FName RetargetSource;
        public FTransform[]? RetargetSourceAssetReferencePose;

        public bool bUseRawDataOnly;

        // UAnimSequenceBase
        public float SequenceLength;
        public float RateScale;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            SequenceLength = GetOrDefault<float>(nameof(SequenceLength));
            RateScale = GetOrDefault(nameof(RateScale), 1.0f);

            NumFrames = GetOrDefault<int>(nameof(NumFrames));
            BoneCompressionSettings = GetOrDefault<FPackageIndex>(nameof(BoneCompressionSettings))?.ResolvedObject;
            AdditiveAnimType = GetOrDefault<EAdditiveAnimationType>(nameof(AdditiveAnimType));
            RetargetSource = GetOrDefault<FName>(nameof(RetargetSource));
            RetargetSourceAssetReferencePose = GetOrDefault<FTransform[]>(nameof(RetargetSourceAssetReferencePose));

            if (BoneCompressionSettings == null && Ar.Game == EGame.GAME_RogueCompany)
            {
                BoneCompressionSettings = new ResolvedLoadedObject(Owner!.Provider!.LoadObject("/Game/Animation/KSAnimBoneCompressionSettings")!);
            }

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
                var compressedData = new FUECompressedAnimData();

                // Part of data were serialized as properties
                compressedData.CompressedByteStream = Ar.ReadArray<byte>();
                if (Ar.Game == EGame.GAME_SeaOfThieves && compressedData.CompressedByteStream.Length == 1 && Ar.Length - Ar.Position > 0)
                {
                    // Sea of Thieves has extra int32 == 1 before the CompressedByteStream
                    Ar.Position -= 1;
                    compressedData.CompressedByteStream = Ar.ReadArray<byte>();
                }

                // Fix layout of "byte swapped" data (workaround for UE4 bug)
                if (compressedData.KeyEncodingFormat == AnimationKeyFormat.AKF_PerTrackCompression && compressedData.CompressedScaleOffsets.OffsetData.Length > 0)
                {
                    /*TArray<uint8> SwappedData;
                    TransferPerTrackData(SwappedData, CompressedByteStream);
                    Exchange(SwappedData, CompressedByteStream);*/
                    throw new NotImplementedException();
                }
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
            var compressedData = new FUECompressedAnimData();
            CompressedDataStructure = compressedData;

            // These fields were serialized as properties in pre-UE4.12 engine version
            compressedData.KeyEncodingFormat = Ar.Read<AnimationKeyFormat>();
            compressedData.TranslationCompressionFormat = Ar.Read<AnimationCompressionFormat>();
            compressedData.RotationCompressionFormat = Ar.Read<AnimationCompressionFormat>();
            compressedData.ScaleCompressionFormat = Ar.Read<AnimationCompressionFormat>();

            compressedData.CompressedTrackOffsets = Ar.ReadArray<int>();
            compressedData.CompressedScaleOffsets = new FCompressedOffsetData(Ar);

            if (Ar.Game >= EGame.GAME_UE4_21)
            {
                // UE4.21+ - added compressed segments; disappeared in 4.23
                var compressedSegments = Ar.ReadArray<FCompressedSegment>();
                if (compressedSegments.Length > 0)
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
                compressedData.CompressedNumberOfFrames = Ar.Read<int>();
            }

            // compressed data
            var numBytes = Ar.Read<int>();
            compressedData.CompressedByteStream = Ar.ReadBytes(numBytes);

            if (Ar.Game >= EGame.GAME_UE4_22)
            {
                var curveCodecPath = Ar.ReadFString();
                var compressedCurveByteStream = Ar.ReadArray<byte>();
            }

            // Fix layout of "byte swapped" data (workaround for UE4 bug)
            if (compressedData.KeyEncodingFormat == AnimationKeyFormat.AKF_PerTrackCompression && compressedData.CompressedScaleOffsets.OffsetData.Length > 0 && Ar.Game < EGame.GAME_UE4_23)
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

            var compressedData = new FUECompressedAnimData();
            CompressedDataStructure = compressedData;

            // Since 4.23, this is FUECompressedAnimData::SerializeCompressedData
            compressedData.KeyEncodingFormat = Ar.Read<AnimationKeyFormat>();
            compressedData.TranslationCompressionFormat = Ar.Read<AnimationCompressionFormat>();
            compressedData.RotationCompressionFormat = Ar.Read<AnimationCompressionFormat>();
            compressedData.ScaleCompressionFormat = Ar.Read<AnimationCompressionFormat>();

            compressedData.CompressedNumberOfFrames = Ar.Read<int>();

            compressedData.CompressedByteStream = new byte[Ar.Read<int>()];
            compressedData.CompressedTrackOffsets = new int[Ar.Read<int>()];
            compressedData.CompressedScaleOffsets.OffsetData = new int[Ar.Read<int>()];
            compressedData.CompressedScaleOffsets.StripSize = Ar.Read<int>();
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

            compressedData.Bind(serializedByteStream);

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

            // Lookup our codecs in our settings assets
            BoneCompressionCodec = BoneCompressionSettings.Load<UAnimBoneCompressionSettings>()!.GetCodec(boneCodecDDCHandle);

            if (BoneCompressionCodec != null)
            {
                CompressedDataStructure = BoneCompressionCodec.AllocateAnimData();
                CompressedDataStructure.SerializeCompressedData(Ar);
                CompressedDataStructure.Bind(serializedByteStream);
            }
            else
            {
                Log.Warning("Unsupported bone compression codec {0}", boneCodecDDCHandle);
            }
        }

        // WARNING: the following functions uses some logic to use either CompressedTrackToSkeletonMapTable or TrackToSkeletonMapTable.
        // This logic should be the same everywhere. Note: CompressedTrackToSkeletonMapTable appeared in UE4.12, so it will always be
        // empty when loading animations from older engines.

        public int GetNumTracks() => CompressedTrackToSkeletonMapTable.Length > 0 ?
            CompressedTrackToSkeletonMapTable.Length :
            TrackToSkeletonMapTable.Length;

        public int GetTrackBoneIndex(int trackIndex) => CompressedTrackToSkeletonMapTable.Length > 0 ?
            CompressedTrackToSkeletonMapTable[trackIndex].BoneTreeIndex :
            TrackToSkeletonMapTable[trackIndex].BoneTreeIndex;

        public int FindTrackForBoneIndex(int boneIndex) {
            var trackMap = CompressedTrackToSkeletonMapTable.Length > 0 ? CompressedTrackToSkeletonMapTable : TrackToSkeletonMapTable;
            for (int trackIndex = 0; trackIndex < trackMap.Length; trackIndex++)
            {
                if (trackMap[trackIndex].BoneTreeIndex == boneIndex)
                    return trackIndex;
            }
            return -1;
        }
    }
}