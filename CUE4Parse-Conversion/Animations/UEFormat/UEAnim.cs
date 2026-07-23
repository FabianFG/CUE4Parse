using CUE4Parse_Conversion.Animations.PSA;
using CUE4Parse_Conversion.UEFormat.Natives;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Animations.UEFormat;

public static class UEAnim
{
    public static byte[] Export(string name, string objectPath, CAnimSet animSet, int sequenceIndex, ExporterOptions options)
    {
        using var pin = new NativePinScope();
        var sequence = animSet.Sequences[sequenceIndex];
        var originalSequence = sequence.OriginalSequence;
        var refSkeleton = animSet.Skeleton.ReferenceSkeleton;

        var metadata = new UEFormatAnimMetadataDesc
        {
            NumFrames = sequence.NumFrames,
            FramesPerSecond = sequence.FramesPerSecond,
            RefPosePath = pin.AllocUtf8(originalSequence.RefPoseSeq?.GetPathName() ?? string.Empty),
            AdditiveAnimType = (byte)originalSequence.AdditiveAnimType,
            RefPoseType = (byte)originalSequence.RefPoseType,
            RefFrameIndex = originalSequence.RefFrameIndex,
        };

        var tracks = new UEFormatTrackDesc[sequence.Tracks.Count];
        var constantAnimation = originalSequence.GetOrDefault<bool>("bConstantAnimation");

        for (var i = 0; i < sequence.Tracks.Count; i++)
        {
            var boneName = refSkeleton.FinalRefBoneInfo[i].Name.Text;
            var track = sequence.Tracks[i];
            var boneTransform = refSkeleton.FinalRefBonePose[i];

            var positionKeys = new List<UEFormatVectorKeyDesc>();
            var rotationKeys = new List<UEFormatQuatKeyDesc>();
            var scaleKeys = new List<UEFormatVectorKeyDesc>();
            FVector? prevPos = null;
            FQuat? prevRot = null;
            FVector? prevScale = null;

            for (var frame = 0; frame < sequence.NumFrames; frame++)
            {
                var translation = boneTransform.Translation;
                var rotation = boneTransform.Rotation;
                var scale = boneTransform.Scale3D;
                if (originalSequence.FindTrackForBoneIndex(i) >= 0)
                {
                    track.GetBoneTransform(frame, sequence.NumFrames, ref rotation, ref translation, ref scale);
                }

                if (constantAnimation)
                {
                    if (prevPos is null || (prevPos != translation && track.KeyPosTime.Contains(frame)))
                    {
                        if (prevPos != null)
                            positionKeys.Add(MakeVectorKey(frame - 1, (FVector)prevPos));
                        positionKeys.Add(MakeVectorKey(frame, translation));
                        prevPos = translation;
                    }

                    if (prevRot is null || (prevRot != rotation && track.KeyQuatTime.Contains(frame)))
                    {
                        if (prevRot != null)
                            rotationKeys.Add(MakeQuatKey(frame - 1, (FQuat)prevRot));
                        rotationKeys.Add(MakeQuatKey(frame, rotation));
                        prevRot = rotation;
                    }

                    if (prevScale is null || (prevScale != scale && track.KeyScaleTime.Contains(frame)))
                    {
                        if (prevScale != null)
                            scaleKeys.Add(MakeVectorKey(frame - 1, (FVector)prevScale));
                        scaleKeys.Add(MakeVectorKey(frame, scale));
                        prevScale = scale;
                    }
                }
                else
                {
                    if (prevPos is null || prevPos != translation)
                    {
                        positionKeys.Add(MakeVectorKey(frame, translation));
                        prevPos = translation;
                    }

                    if (prevRot is null || prevRot != rotation)
                    {
                        rotationKeys.Add(MakeQuatKey(frame, rotation));
                        prevRot = rotation;
                    }

                    if (prevScale is null || prevScale != scale)
                    {
                        scaleKeys.Add(MakeVectorKey(frame, scale));
                        prevScale = scale;
                    }
                }
            }

            var posArray = positionKeys.ToArray();
            var rotArray = rotationKeys.ToArray();
            var scaleArray = scaleKeys.ToArray();

            tracks[i] = new UEFormatTrackDesc
            {
                BoneName = pin.AllocUtf8(boneName),
                PositionKeys = pin.PinArray(posArray),
                PositionKeyCount = posArray.Length,
                RotationKeys = pin.PinArray(rotArray),
                RotationKeyCount = rotArray.Length,
                ScaleKeys = pin.PinArray(scaleArray),
                ScaleKeyCount = scaleArray.Length,
            };
        }

        UEFormatCurveDesc[]? curves = null;
        var floatCurves = originalSequence.CompressedCurveData?.FloatCurves;
        if (floatCurves is { Length: > 0 })
        {
            curves = new UEFormatCurveDesc[floatCurves.Length];
            for (var c = 0; c < floatCurves.Length; c++)
            {
                var floatCurve = floatCurves[c];
                var keys = new UEFormatFloatKeyDesc[floatCurve.FloatCurve.Keys.Length];
                for (var k = 0; k < floatCurve.FloatCurve.Keys.Length; k++)
                {
                    var key = floatCurve.FloatCurve.Keys[k];
                    keys[k] = new UEFormatFloatKeyDesc
                    {
                        Frame = (int)(key.Time * sequence.FramesPerSecond),
                        Value = key.Value,
                    };
                }

                curves[c] = new UEFormatCurveDesc
                {
                    CurveName = pin.AllocUtf8(floatCurve.CurveName.Text),
                    Keys = pin.PinArray(keys),
                    KeyCount = keys.Length,
                };
            }
        }

        var desc = new UEFormatAnimDesc
        {
            Metadata = metadata,
            Tracks = pin.PinArray(tracks),
            TrackCount = tracks.Length,
            Curves = pin.PinArray(curves),
            CurveCount = curves?.Length ?? 0,
        };
        return UEFormatNativeSave.SaveAnim(ref desc, name, objectPath, options, pin);
    }

    private static UEFormatVectorKeyDesc MakeVectorKey(int frame, FVector value) => new()
    {
        Frame = frame,
        Value = UEFormatNativeSave.ToVector(value),
    };

    private static UEFormatQuatKeyDesc MakeQuatKey(int frame, FQuat value) => new()
    {
        Frame = frame,
        Value = UEFormatNativeSave.ToQuat(value),
    };
}
