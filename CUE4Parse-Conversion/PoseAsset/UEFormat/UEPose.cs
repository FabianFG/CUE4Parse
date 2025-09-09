using System;
using System.Collections.Generic;
using CUE4Parse_Conversion.PoseAsset.Conversion;
using CUE4Parse_Conversion.UEFormat;
using CUE4Parse_Conversion.UEFormat.Structs;
using CUE4Parse.UE4.Objects.Engine.Animation;

namespace CUE4Parse_Conversion.PoseAsset.UEFormat;

public class UEPose : UEFormatExport
{
    protected override string Identifier { get; set; } = "UEPOSE";
    
    public UEPose(string name, CPoseAsset poseAsset, ExporterOptions options) : base(name, options)
    {
        using (var posesChunk = new FDataChunk("POSES", poseAsset.Poses.Count))
        {
            foreach (var pose in poseAsset.Poses)
            {
                posesChunk.WriteFString(pose.PoseName);

                posesChunk.WriteArray(pose.Keys, (writer, key) =>
                {
                    writer.WriteFString(key.BoneName);
                    key.Location.Serialize(writer);
                    key.Rotation.Serialize(writer);
                    key.Scale.Serialize(writer);
                });

                var curveToInfluence = new Dictionary<int, float>();
                for (var curveIndex = 0; curveIndex < pose.CurveData.Length; curveIndex++)
                {
                    var curveValue = pose.CurveData[curveIndex];
                    if (Math.Abs(curveValue) < 0.001) continue;

                    curveToInfluence[curveIndex] = curveValue;
                }
                
                posesChunk.WriteArray(curveToInfluence, (writer, kvp) =>
                {
                    writer.Write(kvp.Key);
                    writer.Write(kvp.Value);
                });
            }
            
            posesChunk.Serialize(Ar);
        }

        using (var curvesChunk = new FDataChunk("CURVES", poseAsset.CurveNames.Count))
        {
            foreach (var curveName in poseAsset.CurveNames)
            {
                curvesChunk.WriteFString(curveName);
            }
            curvesChunk.Serialize(Ar);
        }
    }
}