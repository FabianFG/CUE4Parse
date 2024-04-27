using System.Collections.Generic;
using CUE4Parse_Conversion.Animations.PSA;
using CUE4Parse_Conversion.UEFormat;
using CUE4Parse_Conversion.UEFormat.Structs;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Animations.UEFormat;

public class UEAnim : UEFormatExport
{
    protected override string Identifier { get; set; } = "UEANIM";
    
    public UEAnim(string name, CAnimSet animSet, int sequenceIndex, ExporterOptions options) : base(name, options)
    {
        var sequence = animSet.Sequences[sequenceIndex];
        var originalSequence = sequence.OriginalSequence;
        Ar.Write(sequence.NumFrames);
        Ar.Write(sequence.FramesPerSecond);

        var refSkeleton = animSet.Skeleton.ReferenceSkeleton;
        using (var trackChunk = new FDataChunk("TRACKS", sequence.Tracks.Count))
        {
            for (var i = 0; i < sequence.Tracks.Count; i++)
            {
                var boneName = refSkeleton.FinalRefBoneInfo[i].Name.Text;
                trackChunk.WriteFString(boneName);
                
                var track = sequence.Tracks[i];
                var boneTransform = refSkeleton.FinalRefBonePose[i];
                
                var positionKeys = new List<FVectorKey>();
                var rotationKeys = new List<FQuatKey>();
                var scaleKeys = new List<FVectorKey>();
                FVector? prevPos = null;
                FQuat? prevRot = null;
                FVector? prevScale = null;
                for (var frame = 0; frame < sequence.NumFrames; frame++)
                {
                    var translation = boneTransform.Translation;
                    var rotation = boneTransform.Rotation;
                    var scale = boneTransform.Scale3D;
                    if (sequence.OriginalSequence.FindTrackForBoneIndex(i) >= 0)
                    {
                        track.GetBoneTransform(frame, sequence.NumFrames, ref rotation, ref translation, ref scale);
                    }
                    
                    rotation.Y = -rotation.Y;
                    rotation.W = -rotation.W;
                    translation.Y = -translation.Y;

                    // dupe key reduction, could be better but it works for now
                    if (prevPos is null || prevPos != translation)
                    {
                        positionKeys.Add(new FVectorKey(frame, translation));
                        prevPos = translation;
                    }
                    
                    if (prevRot is null || prevRot != rotation)
                    {
                        rotationKeys.Add(new FQuatKey(frame, rotation));
                        prevRot = rotation;
                    }
                    
                    if (prevScale is null || prevScale != scale)
                    {
                        scaleKeys.Add(new FVectorKey(frame, scale));
                        prevScale = scale;
                    }
                }
                
                trackChunk.WriteArray(positionKeys);
                trackChunk.WriteArray(rotationKeys);
                trackChunk.WriteArray(scaleKeys);
            }
            
            trackChunk.Serialize(Ar);
        }

        var floatCurves = originalSequence.CompressedCurveData.FloatCurves;
        if (floatCurves is not null)
        {
            using var curveChunk = new FDataChunk("CURVES", floatCurves.Length);
            
            foreach (var floatCurve in floatCurves)
            {
                // TODO serialize more data for better accuracy
                curveChunk.WriteFString(floatCurve.CurveName.Text);
                curveChunk.Write(floatCurve.FloatCurve.Keys.Length);
                foreach (var floatCurveKey in floatCurve.FloatCurve.Keys)
                {
                    var key = new FFloatKey((int) (floatCurveKey.Time * sequence.FramesPerSecond), floatCurveKey.Value);
                    key.Serialize(curveChunk);
                }
            }
            
            curveChunk.Serialize(Ar);
        }
        
    }
}