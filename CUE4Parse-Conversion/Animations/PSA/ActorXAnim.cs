using CUE4Parse_Conversion.ActorX;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Animations.PSA;

public class ActorXAnim
{
    private FArchiveWriter Ar;
    private readonly ExporterOptions Options;
    
    public ActorXAnim(ExporterOptions options)
    {
        Options = options;
        Ar = new FArchiveWriter();
    }

    public ActorXAnim(CAnimSet anim, int seqIdx, ExporterOptions options) : this(options)
    {
        DoExportPsa(anim, seqIdx);
    }
    
    public void Save(FArchiveWriter archive)
    {
        archive.Write(Ar.GetBuffer());
    }
    
    private void DoExportPsa(CAnimSet anim, int seqIdx)
    {
        var mainHdr = new VChunkHeader { TypeFlag = Constants.PSA_VERSION };
        Ar.SerializeChunkHeader(mainHdr, "ANIMHEAD");

        var numBones = anim.Skeleton.BoneCount;
        var boneHdr = new VChunkHeader { DataCount = numBones, DataSize = Constants.FNamedBoneBinary_SIZE };
        Ar.SerializeChunkHeader(boneHdr, "BONENAMES");
        for (var boneIndex = 0; boneIndex < numBones; boneIndex++)
        {
            var boneInfo = anim.Skeleton.ReferenceSkeleton.FinalRefBoneInfo[boneIndex];
            var boneTransform = anim.Skeleton.ReferenceSkeleton.FinalRefBonePose[boneIndex];
            var bone = new FNamedBoneBinary
            {
                Name = boneInfo.Name.Text,
                Flags = 0, // reserved
                NumChildren = 0, // unknown here
                ParentIndex = boneInfo.ParentIndex, // unknown for UAnimSet?? edit 2023: no
                BonePos =
                {
                    Orientation = boneTransform.Rotation,
                    Position = boneTransform.Translation,
                    Size = boneTransform.Scale3D,
                    Length = 1.0f
                }
            };
            bone.Serialize(Ar);
        }

        var sequence = anim.Sequences[seqIdx];
        var animHdr = new VChunkHeader { DataCount = 1, DataSize = Constants.ANIM_INFO_SIZE };
        Ar.SerializeChunkHeader(animHdr, "ANIMINFO");
        var animInfo = new AnimInfoBinary
        {
            Name = sequence.Name,
            Group = /*??S.Groups.Length > 0 ? S.Groups[0] :*/ "None",
            TotalBones = numBones,
            RootInclude = 0, // unused
            KeyCompressionStyle = 0, // reserved
            KeyQuotum = sequence.NumFrames * numBones, // reserved, but fill with keys count
            KeyReduction = 0, // reserved
            TrackTime = sequence.NumFrames,
            AnimRate = sequence.FramesPerSecond,
            StartBone = 0, // reserved
            FirstRawFrame = 0, // useless, but used in UnrealEd when importing
            NumRawFrames = sequence.NumFrames
        };
        animInfo.Serialize(Ar);

        var keyHdr = new VChunkHeader { DataCount = sequence.NumFrames * numBones, DataSize = Constants.VQuatAnimKey_SIZE };
        Ar.SerializeChunkHeader(keyHdr, "ANIMKEYS");
        for (int frame = 0; frame < sequence.NumFrames; frame++)
        {
            for (int boneIndex = 0; boneIndex < numBones; boneIndex++)
            {
                var boneTransform = anim.Skeleton.ReferenceSkeleton.FinalRefBonePose[boneIndex];
                var key = new VQuatAnimKey // scale me you fucking idiot
                {
                    Position = boneTransform.Translation,
                    Orientation = boneTransform.Rotation,
                    Time = 1
                };
                if (sequence.OriginalSequence.FindTrackForBoneIndex(boneIndex) >= 0)
                {
                    var eeehhhohhhhh = FVector.OneVector;
                    sequence.Tracks[boneIndex].GetBoneTransform(frame, sequence.NumFrames, ref key.Orientation, ref key.Position, ref eeehhhohhhhh);
                }

                // MIRROR_MESH
                key.Orientation.Y *= -1;
                if (boneIndex == 0) key.Orientation.W *= -1; // because the importer has invert enabled by default...
                key.Position.Y *= -1;
                key.Serialize(Ar);
            }
        }

        // UE3 source code reference: UEditorEngine::ImportPSAIntoAnimSet()
        // The function doesn't perform any checks for chunk names etc, so we're very restricted in
        // using very strict order of chunks. If main chunk has version (TypeFlag) at least 20090127,
        // importer will always read "SCALEKEYS" chunk.
        // edit 2023: can we stop breaking FTransform in different chunks???
        if (mainHdr.TypeFlag >= 20090127)
        {
            var scaleKeysHdr = new VChunkHeader { DataCount = keyHdr.DataCount, DataSize = Constants.VScaleAnimKey_SIZE };
            Ar.SerializeChunkHeader(scaleKeysHdr, "SCALEKEYS");
            for (int frame = 0; frame < sequence.NumFrames; frame++)
            {
                for (int boneIndex = 0; boneIndex < numBones; boneIndex++)
                {
                    FVector boneScale = anim.Skeleton.ReferenceSkeleton.FinalRefBonePose[boneIndex].Scale3D;
                    if (sequence.OriginalSequence.FindTrackForBoneIndex(boneIndex) >= 0)
                    {
                        var bonePosition = FVector.ZeroVector;
                        var boneOrientation = FQuat.Identity;
                        sequence.Tracks[boneIndex].GetBoneTransform(frame, sequence.NumFrames, ref boneOrientation, ref bonePosition, ref boneScale);
                    }

                    var key = new VScaleAnimKey
                    {
                        ScaleVector = boneScale,
                        Time = 1
                    };
                    key.Serialize(Ar);
                }
            }
        }
    }
}