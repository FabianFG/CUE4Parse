using System;
using System.Collections.Generic;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Writers.ActorX.Structs.Animations;
using CUE4Parse_Conversion.Writers.UEFormat.Structs;

namespace CUE4Parse_Conversion.Writers.UEFormat;

public sealed class UEPose : UEFormatExport
{
    protected override string Identifier => "UEPOSE";

    public UEPose(string name, CPoseAsset poseAsset, ExportOptions options) : base(name, options)
    {
        SerializePoses(poseAsset);
        SerializeCurves(poseAsset);
    }

    private void SerializePoses(CPoseAsset poseAsset)
    {
        using var posesChunk = new FDataChunk("POSES", poseAsset.Poses.Count);

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

    private void SerializeCurves(CPoseAsset poseAsset)
    {
        using var curvesChunk = new FDataChunk("CURVES", poseAsset.CurveNames.Count);
        foreach (var curveName in poseAsset.CurveNames)
        {
            curvesChunk.WriteFString(curveName);
        }
        curvesChunk.Serialize(Ar);
    }
}
