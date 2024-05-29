using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Exports.Animation.ACL;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using Newtonsoft.Json;
using Serilog;
using static CUE4Parse.UE4.Assets.Exports.Animation.AnimationCompressionFormat;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public class UAnimSequence : UAnimSequenceBase
    {
        public int NumFrames;
        public FTrackToSkeletonMap[]? TrackToSkeletonMapTable; // used for raw data
        public FRawAnimSequenceTrack[] RawAnimationData;
        public ResolvedObject? BoneCompressionSettings; // UAnimBoneCompressionSettings
        public ResolvedObject? CurveCompressionSettings; // UAnimCurveCompressionSettings

        #region FCompressedAnimSequence CompressedData
        public FTrackToSkeletonMap[] CompressedTrackToSkeletonMapTable; // used for compressed data, missing before 4.12
        public FSmartName[] CompressedCurveNames;
        //public byte[] CompressedByteStream; The actual data will be in CompressedDataStructure, no need to store as field
        public byte[]? CompressedCurveByteStream;
        public FRawCurveTracks CompressedCurveData; // disappeared in 4.23
        public ICompressedAnimData CompressedDataStructure;
        public UAnimBoneCompressionCodec? BoneCompressionCodec;
        public UAnimCurveCompressionCodec? CurveCompressionCodec;
        public int CompressedRawDataSize;
        #endregion

        public EAdditiveAnimationType AdditiveAnimType;
        public EAdditiveBasePoseType RefPoseType;
        public ResolvedObject? RefPoseSeq;
        public int RefFrameIndex;
        public FName RetargetSource;
        public FTransform[]? RetargetSourceAssetReferencePose;

        public bool bUseRawDataOnly;
        public bool EnsuredCurveData;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            NumFrames = GetOrDefault<int>(nameof(NumFrames));
            BoneCompressionSettings = GetOrDefault<ResolvedObject>(nameof(BoneCompressionSettings));
            CurveCompressionSettings = GetOrDefault<ResolvedObject>(nameof(CurveCompressionSettings));
            AdditiveAnimType = GetOrDefault<EAdditiveAnimationType>(nameof(AdditiveAnimType));
            RefPoseType = GetOrDefault<EAdditiveBasePoseType>(nameof(RefPoseType));
            RefPoseSeq = GetOrDefault<ResolvedObject>(nameof(RefPoseSeq));
            RefFrameIndex = GetOrDefault(nameof(RefFrameIndex), 0);
            RetargetSource = GetOrDefault<FName>(nameof(RetargetSource));
            RetargetSourceAssetReferencePose = GetOrDefault<FTransform[]>(nameof(RetargetSourceAssetReferencePose));

            if (BoneCompressionSettings == null && Ar.Game == EGame.GAME_RogueCompany)
            {
                BoneCompressionSettings = new ResolvedLoadedObject(Owner!.Provider!.LoadObject("/Game/Animation/KSAnimBoneCompressionSettings.KSAnimBoneCompressionSettings"));
            }

            var stripFlags = new FStripDataFlags(Ar);
            if (!stripFlags.IsEditorDataStripped())
            {
                RawAnimationData = Ar.ReadArray(() => new FRawAnimSequenceTrack(Ar));
                if (Ar.Ver >= EUnrealEngineObjectUE4Version.ANIMATION_ADD_TRACKCURVES)
                {
                    if (FUE5MainStreamObjectVersion.Get(Ar) < FUE5MainStreamObjectVersion.Type.RemovingSourceAnimationData)
                    {
                        var sourceRawAnimationData = Ar.ReadArray(() => new FRawAnimSequenceTrack(Ar));
                        if (sourceRawAnimationData.Length > 0)
                        {
                            // Set RawAnimationData to Source
                            RawAnimationData = sourceRawAnimationData;
                        }
                    }
                }
            }

            if (FFrameworkObjectVersion.Get(Ar) < FFrameworkObjectVersion.Type.MoveCompressedAnimDataToTheDDC)
            {
                var compressedData = new FUECompressedAnimData();
                CompressedDataStructure = compressedData;

                // Part of data were serialized as properties
                compressedData.CompressedByteStream = Ar.ReadBytes(Ar.Read<int>());
                if (Ar.Game == EGame.GAME_SeaOfThieves && compressedData.CompressedByteStream.Length == 1 && Ar.Length - Ar.Position > 0)
                {
                    // Sea of Thieves has extra int32 == 1 before the CompressedByteStream
                    Ar.Position -= 1;
                    compressedData.CompressedByteStream = Ar.ReadBytes(Ar.Read<int>());
                }

                // Fix layout of "byte swapped" data (workaround for UE4 bug)
                if (compressedData.KeyEncodingFormat == AnimationKeyFormat.AKF_PerTrackCompression && compressedData.CompressedScaleOffsets.OffsetData.Length > 0)
                {
                    compressedData.CompressedByteStream = TransferPerTrackData(compressedData.CompressedByteStream);
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

            EnsuredCurveData = EnsureCurveData();
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            // Follow field order of FCompressedAnimSequence CompressedData

            if (CompressedTrackToSkeletonMapTable is { Length: > 0 })
            {
                writer.WritePropertyName("CompressedTrackToSkeletonMapTable");
                writer.WriteStartArray();
                foreach (var trackToSkeletonMap in CompressedTrackToSkeletonMapTable)
                {
                    writer.WriteValue(trackToSkeletonMap.BoneTreeIndex);
                }
                writer.WriteEndArray();
            }

            if (CompressedCurveNames is { Length: > 0 })
            {
                writer.WritePropertyName("CompressedCurveNames");
                serializer.Serialize(writer, CompressedCurveNames);
            }

            /*if (CompressedByteStream is { Length: > 0 })
            {
                writer.WritePropertyName("CompressedByteStream");
                writer.WriteValue(CompressedByteStream);
            }*/

            /*if (CompressedCurveByteStream is { Length: > 0 })
            {
                writer.WritePropertyName("CompressedCurveByteStream");
                writer.WriteValue(CompressedCurveByteStream);
            }*/

            if (EnsuredCurveData)
            {
                writer.WritePropertyName("CompressedCurveData");
                serializer.Serialize(writer, CompressedCurveData);
            }

            if (CompressedDataStructure != null)
            {
                writer.WritePropertyName("CompressedDataStructure");
                serializer.Serialize(writer, CompressedDataStructure);
            }

            if (BoneCompressionCodec != null)
            {
                var asReference = new ResolvedLoadedObject(BoneCompressionCodec);
                writer.WritePropertyName("BoneCompressionCodec");
                serializer.Serialize(writer, asReference);
            }

            if (CurveCompressionCodec != null)
            {
                var asReference = new ResolvedLoadedObject(CurveCompressionCodec);
                writer.WritePropertyName("CurveCompressionCodec");
                serializer.Serialize(writer, asReference);
            }

            if (CompressedRawDataSize > 0)
            {
                writer.WritePropertyName("CompressedRawDataSize");
                writer.WriteValue(CompressedRawDataSize);
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
                CompressedCurveData = new FRawCurveTracks(new FStructFallback(Ar, "RawCurveTracks"));
            }
            else
            {
                CompressedCurveNames = Ar.ReadArray(() => new FSmartName(Ar));
            }

            if (Ar.Versions["AnimSequence.HasCompressedRawSize"])
            {
                // UE4.17+
                CompressedRawDataSize = Ar.Read<int>();
            }

            if (Ar.Game >= EGame.GAME_UE4_22)
            {
                compressedData.CompressedNumberOfFrames = Ar.Read<int>();
            }

            var nameIndex = Ar.Read<int>();//ACL thing - KeyEncodingFormat FName
            Ar.Position -= 4;
            if (nameIndex >= 0 && nameIndex < Ar.Owner.NameMap.Length)
            {
                var format = Ar.ReadFName();
                if ("AKF_" + format.Text != compressedData.KeyEncodingFormat.ToString() && !format.Text.StartsWith("ACL")) Ar.Position -= 8;
                compressedData.CompressedByteStream = Ar.ReadBytes(Ar.Read<int>());
                if (format.Text.StartsWith("ACL"))
                {
                    CompressedDataStructure = new UAnimBoneCompressionCodec_ACLSafe().AllocateAnimData();
                    CompressedDataStructure.Bind(compressedData.CompressedByteStream);
                }
            }
            else
            {
                // compressed data
                compressedData.CompressedByteStream = Ar.ReadBytes(Ar.Read<int>());
            }

            if (Ar.Game >= EGame.GAME_UE4_22)
            {
                var curveCodecPath = Ar.ReadFString();
                CurveCompressionCodec = CurveCompressionSettings?.Load<UAnimCurveCompressionSettings>()?.GetCodec(curveCodecPath);
                CompressedCurveByteStream = Ar.ReadBytes(Ar.Read<int>());
            }

            // Fix layout of "byte swapped" data (workaround for UE4 bug)
            if (compressedData.KeyEncodingFormat == AnimationKeyFormat.AKF_PerTrackCompression && compressedData.CompressedScaleOffsets.OffsetData.Length > 0 && Ar.Game < EGame.GAME_UE4_23)
            {
                compressedData.CompressedByteStream = TransferPerTrackData(compressedData.CompressedByteStream);
            }
        }

        // UE4.23-4.24 has changed compressed data layout for streaming, so it's worth making a separate
        // serializer function for it.
        private void SerializeCompressedData2(FAssetArchive Ar)
        {
            CompressedRawDataSize = Ar.Read<int>();
            CompressedTrackToSkeletonMapTable = Ar.ReadArray<FTrackToSkeletonMap>();
            CompressedCurveNames = Ar.ReadArray(() => new FSmartName(Ar));

            var compressedData = new FUECompressedAnimData();
            CompressedDataStructure = compressedData;
            CompressedDataStructure.SerializeCompressedData(Ar);

            var serializedByteStream = ReadSerializedByteStream(Ar);
            compressedData.Bind(serializedByteStream);
            NumFrames = CompressedDataStructure.CompressedNumberOfFrames;

            var curveCodecPath = Ar.ReadFString();
            CurveCompressionCodec = CurveCompressionSettings?.Load<UAnimCurveCompressionSettings>()?.GetCodec(curveCodecPath);
            CompressedCurveByteStream = Ar.ReadBytes(Ar.Read<int>());
        }

        // UE4.25 has changed data layout, and therefore serialization order has been changed too.
        // In UE4.25 serialization is done in FCompressedAnimSequence::SerializeCompressedData().
        private void SerializeCompressedData3(FAssetArchive Ar)
        {
            CompressedRawDataSize = Ar.Read<int>();
            CompressedTrackToSkeletonMapTable = Ar.ReadArray<FTrackToSkeletonMap>();
            CompressedCurveNames = Ar.ReadArray(() => new FSmartName(Ar));

            var serializedByteStream = ReadSerializedByteStream(Ar);

            var boneCodecDDCHandle = Ar.ReadFString();
            var curveCodecPath = Ar.ReadFString();

            var numCurveBytes = Ar.Read<int>();
            CompressedCurveByteStream = Ar.ReadBytes(numCurveBytes);

            // Lookup our codecs in our settings assets
            BoneCompressionCodec = BoneCompressionSettings?.Load<UAnimBoneCompressionSettings>()?.GetCodec(boneCodecDDCHandle);
            CurveCompressionCodec = CurveCompressionSettings?.Load<UAnimCurveCompressionSettings>()?.GetCodec(curveCodecPath);

            if (BoneCompressionCodec != null)
            {
                CompressedDataStructure = BoneCompressionCodec.AllocateAnimData();
                CompressedDataStructure.SerializeCompressedData(Ar);
                CompressedDataStructure.Bind(serializedByteStream);
                NumFrames = CompressedDataStructure.CompressedNumberOfFrames;
            }
            else
            {
                Log.Warning("Unknown bone compression codec {0}", boneCodecDDCHandle);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] ReadSerializedByteStream(FAssetArchive Ar)
        {
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
            return serializedByteStream;
        }

        public bool IsValidAdditive()
        {
            if (AdditiveAnimType == EAdditiveAnimationType.AAT_None) return false;
            return RefPoseType switch
            {
                EAdditiveBasePoseType.ABPT_RefPose => true,
                EAdditiveBasePoseType.ABPT_AnimScaled => RefPoseSeq != null && RefPoseSeq.Name.Text != Name,
                EAdditiveBasePoseType.ABPT_AnimFrame => RefPoseSeq != null && RefPoseSeq.Name.Text != Name && RefFrameIndex >= 0,
                EAdditiveBasePoseType.ABPT_LocalAnimFrame => RefFrameIndex >= 0,
                _ => false
            };
        }

        // WARNING: the following functions uses some logic to use either CompressedTrackToSkeletonMapTable or TrackToSkeletonMapTable.
        // This logic should be the same everywhere. Note: CompressedTrackToSkeletonMapTable appeared in UE4.12, so it will always be
        // empty when loading animations from older engines.

        public int GetNumTracks() => CompressedTrackToSkeletonMapTable.Length > 0 ?
            CompressedTrackToSkeletonMapTable.Length :
            TrackToSkeletonMapTable?.Length ?? 0;

        public int GetTrackBoneIndex(int trackIndex) => CompressedTrackToSkeletonMapTable.Length > 0 ?
            CompressedTrackToSkeletonMapTable[trackIndex].BoneTreeIndex :
            TrackToSkeletonMapTable?[trackIndex].BoneTreeIndex ?? -1;

        public FTrackToSkeletonMap[] GetTrackMap() => CompressedTrackToSkeletonMapTable.Length > 0 ? CompressedTrackToSkeletonMapTable : TrackToSkeletonMapTable ?? [];

        public int FindTrackForBoneIndex(int boneIndex) {
            var trackMap = GetTrackMap();
            for (var trackIndex = 0; trackIndex < trackMap.Length; trackIndex++)
            {
                if (trackMap[trackIndex].BoneTreeIndex == boneIndex)
                    return trackIndex;
            }
            return -1;
        }

        private static readonly int[] NumComponentsPerMask = { 3, 1, 1, 2, 1, 2, 2, 3 }; // number of identity bits in value, 0 == all bits

        private byte[] TransferPerTrackData(byte[] src)
        {
            var dst = new byte[src.Length];

            var compressedData = (FUECompressedAnimData) CompressedDataStructure;
            var compressedTrackOffsets = compressedData.CompressedTrackOffsets;
            var compressedScaleOffsets = compressedData.CompressedScaleOffsets;

            var numTracks = compressedTrackOffsets.Length / 2;

            var srcOffset = 0;

            for (var trackIndex = 0; trackIndex < numTracks; trackIndex++)
            {
                for (var kind = 0; kind < 3; kind++)
                {
                    // Get track offset
                    var offset = 0;
                    switch (kind)
                    {
                        case 0: // translation data
                            offset = compressedTrackOffsets[trackIndex * 2];
                            break;
                        case 1: // rotation data
                            offset = compressedTrackOffsets[trackIndex * 2 + 1];
                            break;
                        default: // case 2 - scale data
                            offset = compressedScaleOffsets.GetOffsetData(trackIndex, 0);
                            break;
                    }
                    if (offset == -1)
                    {
                        continue;
                    }

                    var dstOffset = offset;

                    // Copy data

                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    void Copy(int size)
                    {
                        Buffer.BlockCopy(src, srcOffset, dst, dstOffset, size);
                        srcOffset += size;
                        dstOffset += size;
                    }

                    // Decode animation header
                    var packedInfo = BitConverter.ToUInt32(src, srcOffset);
                    Copy(sizeof(uint));

                    var keyFormat = (AnimationCompressionFormat) (packedInfo >> 28);
                    var componentMask = (int) ((packedInfo >> 24) & 0xF);
                    var numKeys = (int) (packedInfo & 0xFFFFFF);
                    var hasTimeTracks = (componentMask & 8) != 0;

                    var numComponents = NumComponentsPerMask[componentMask & 7];

                    // mins/randes
                    if (keyFormat == ACF_IntervalFixed32NoW)
                    {
                        Copy(sizeof(float) * numComponents * 2);
                    }

                    // keys
                    switch (keyFormat)
                    {
                        case ACF_Float96NoW:
                            Copy(sizeof(float) * numComponents * numKeys);
                            break;
                        case ACF_Fixed48NoW:
                            Copy(sizeof(ushort) * numComponents * numKeys);
                            break;
                        case ACF_IntervalFixed32NoW:
                        case ACF_Fixed32NoW:
                        case ACF_Float32NoW:
                            Copy(sizeof(uint) * numKeys); // always stored full data, ComponentMask used only for mins/ranges
                            break;
                        case ACF_Identity:
                            // nothing
                            break;
                    }

                    // time data
                    if (hasTimeTracks)
                    {
                        // padding
                        var currentOffset = dstOffset;
                        var alignedOffset = currentOffset.Align(4);
                        if (alignedOffset != currentOffset)
                        {
                            Copy(alignedOffset - currentOffset);
                        }
                        // copy time
                        Copy((NumFrames < 256 ? sizeof(byte) : sizeof(ushort)) * numKeys);
                    }

                    {
                        // align to 4 bytes
                        var currentOffset = dstOffset;
                        var alignedOffset = currentOffset.Align(4);
                        Trace.Assert(alignedOffset <= dst.Length);
                        if (alignedOffset != currentOffset)
                        {
                            Copy(alignedOffset - currentOffset);
                        }
                    }
                }
            }

            return dst;
        }

        private bool EnsureCurveData()
        {
            if (CompressedCurveData.FloatCurves == null && CurveCompressionCodec != null)
            {
                CompressedCurveData.FloatCurves = CurveCompressionCodec.ConvertCurves(this);
                return true;
            }
            return false;
        }
    }
}
