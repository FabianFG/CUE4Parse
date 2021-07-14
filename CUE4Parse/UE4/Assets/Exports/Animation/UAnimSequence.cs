using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Versions;
using Serilog;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public class UAnimSequence : UObject
    {
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
            /*if (Ar.Game == EGame.GAME_MontereySetup)
            {
                var guid = Ar.Read<FGuid>();
            }*/

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

            if (numBytes > 0)
            {
                CompressedByteStream = Ar.ReadBytes(numBytes);
            }

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

        private void SerializeCompressedData2(FAssetArchive Ar)
        {
            throw new NotImplementedException();
        }

        private void SerializeCompressedData3(FAssetArchive Ar)
        {
            throw new NotImplementedException();
        }
    }
}