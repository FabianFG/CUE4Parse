using System.Collections.Generic;
using System.Linq;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Writers.ActorX.Structs.Animations;
using CUE4Parse_Conversion.Writers.UEFormat.Structs;
using CUE4Parse_Conversion.Writers.UEFormat.Structs.Animations;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Writers.UEFormat;

public sealed class UEAnim : UEFormatExport
{
    protected override string Identifier => "UEANIM";

    public UEAnim(string name, string objectPath, CAnimSet animSet, int sequenceIndex, ExportOptions options)
        : base(name, objectPath, options)
    {
        var sequence = animSet.Sequences[sequenceIndex];
        var original = sequence.OriginalSequence;

        WriteRoot(root =>
        {
            WriteMetadata(root, sequence, original);
            WriteTracks(root, sequence, original, animSet.Skeleton.ReferenceSkeleton);

            if (original.CompressedCurveData?.FloatCurves is { Length: > 0 } floatCurves)
                WriteCurves(root, floatCurves, sequence.FramesPerSecond);
        });
    }

    public UEAnim(string name, string objectPath, UAnimStreamable animStreamable, ExportOptions options)
        : base(name, objectPath, options)
    {
        var framesPerSecond = animStreamable.NumFrames / animStreamable.SequenceLength;

        WriteRoot(root =>
        {
            root.AddAttribute("METADATA", attr =>
            {
                attr.Write(animStreamable.NumFrames);
                attr.Write(framesPerSecond);
                attr.WriteFString(string.Empty);
                attr.Write((byte) EAdditiveAnimationType.AAT_None);
                attr.Write((byte) EAdditiveBasePoseType.ABPT_None);
                attr.Write(0);
            });

            if (animStreamable.RawCurveData?.FloatCurves is { Length: > 0 } floatCurves)
                WriteCurves(root, floatCurves, framesPerSecond);
        });
    }

    private static void WriteMetadata(FDataAttributeSet root, CAnimSequence sequence, UAnimSequence original)
    {
        root.AddAttribute("METADATA", attr =>
        {
            attr.Write(sequence.NumFrames);
            attr.Write(sequence.FramesPerSecond);
            attr.WriteFString(original.RefPoseSeq?.GetPathName() ?? string.Empty);
            attr.Write((byte) original.AdditiveAnimType);
            attr.Write((byte) original.RefPoseType);
            attr.Write(original.RefFrameIndex);
        });
    }

    private static void WriteTracks(
        FDataAttributeSet root,
        CAnimSequence sequence,
        UAnimSequence original,
        FReferenceSkeleton refSkeleton)
    {
        root.AddAttribute("TRACKS", attr =>
        {
            attr.WriteArray(sequence.Tracks, (writer, track, i) =>
            {
                writer.WriteFString(refSkeleton.FinalRefBoneInfo[i].Name.Text);

                var (positions, rotations, scales) = SampleTrackKeys(
                    track,
                    refSkeleton.FinalRefBonePose[i],
                    sequence,
                    original,
                    i);

                writer.WriteArray(positions);
                writer.WriteArray(rotations);
                writer.WriteArray(scales);
            });
        });
    }

    private static void WriteCurves(FDataAttributeSet root, FFloatCurve[] floatCurves, float framesPerSecond)
    {
        root.AddAttribute("CURVES", attr => attr.WriteArray(floatCurves, (writer, floatCurve) =>
        {
            writer.WriteFString(floatCurve.CurveName.Text);
            writer.WriteArray(floatCurve.FloatCurve.Keys, key =>
            {
                new FFloatKey((int) (key.Time * framesPerSecond), key.Value).Serialize(writer);
            });
        }));
    }

    private static (List<FVectorKey> Positions, List<FQuatKey> Rotations, List<FVectorKey> Scales) SampleTrackKeys(
        CAnimTrack track,
        FTransform boneTransform,
        CAnimSequence sequence,
        UAnimSequence original,
        int boneIndex)
    {
        var positions = new List<FVectorKey>();
        var rotations = new List<FQuatKey>();
        var scales = new List<FVectorKey>();

        FVector? prevPos = null;
        FQuat? prevRot = null;
        FVector? prevScale = null;
        var constant = original.GetOrDefault<bool>("bConstantAnimation");
        var hasTrack = original.FindTrackForBoneIndex(boneIndex) >= 0;

        for (var frame = 0; frame < sequence.NumFrames; frame++)
        {
            var translation = boneTransform.Translation;
            var rotation = boneTransform.Rotation;
            var scale = boneTransform.Scale3D;
            if (hasTrack)
                track.GetBoneTransform(frame, sequence.NumFrames, ref rotation, ref translation, ref scale);

            if (constant)
            {
                AppendConstantKey(positions, ref prevPos, frame, translation, track.KeyPosTime);
                AppendConstantKey(rotations, ref prevRot, frame, rotation, track.KeyQuatTime);
                AppendConstantKey(scales, ref prevScale, frame, scale, track.KeyScaleTime);
            }
            else
            {
                AppendChangedKey(positions, ref prevPos, frame, translation);
                AppendChangedKey(rotations, ref prevRot, frame, rotation);
                AppendChangedKey(scales, ref prevScale, frame, scale);
            }
        }

        return (positions, rotations, scales);
    }

    private static void AppendChangedKey(List<FVectorKey> keys, ref FVector? prev, int frame, FVector value)
    {
        if (prev == value) return;
        keys.Add(new FVectorKey(frame, value));
        prev = value;
    }

    private static void AppendChangedKey(List<FQuatKey> keys, ref FQuat? prev, int frame, FQuat value)
    {
        if (prev == value) return;
        keys.Add(new FQuatKey(frame, value));
        prev = value;
    }

    private static void AppendConstantKey(List<FVectorKey> keys, ref FVector? prev, int frame, FVector value, ICollection<float> keyTimes)
    {
        if (prev is not null && (prev == value || !keyTimes.Contains(frame))) return;
        if (prev is not null)
            keys.Add(new FVectorKey(frame - 1, (FVector) prev));
        keys.Add(new FVectorKey(frame, value));
        prev = value;
    }

    private static void AppendConstantKey(List<FQuatKey> keys, ref FQuat? prev, int frame, FQuat value, ICollection<float> keyTimes)
    {
        if (prev is not null && (prev == value || !keyTimes.Contains(frame))) return;
        if (prev is not null)
            keys.Add(new FQuatKey(frame - 1, (FQuat) prev));
        keys.Add(new FQuatKey(frame, value));
        prev = value;
    }
}
