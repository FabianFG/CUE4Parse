using System;
using System.Collections.Generic;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Writers.ActorX.Structs.Animations;
using CUE4Parse_Conversion.Writers.UEFormat.Structs;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Writers.UEFormat;

public sealed class UEPose : UEFormatExport
{
    protected override string Identifier => "UEPOSE";

    public UEPose(string name, string objectPath, CPoseAsset poseAsset, ExportOptions options)
        : base(name, objectPath, options)
    {
        WriteRoot(root =>
        {
            root.AddAttribute("POSES", attr => attr.WriteArray(poseAsset.Poses, WritePose));
            root.AddAttribute("CURVES", attr => attr.WriteArray(poseAsset.CurveNames, attr.WriteFString));
        });
    }

    private static void WritePose(FArchiveWriter writer, CPoseData pose)
    {
        writer.WriteFString(pose.PoseName);

        writer.WriteArray(pose.Keys, (writer, key) =>
        {
            writer.WriteFString(key.BoneName);
            key.Location.Serialize(writer);
            key.Rotation.Serialize(writer);
            key.Scale.Serialize(writer);
        });

        var influences = new List<KeyValuePair<int, float>>();
        for (var curveIndex = 0; curveIndex < pose.CurveData.Length; curveIndex++)
        {
            var curveValue = pose.CurveData[curveIndex];
            if (Math.Abs(curveValue) < 0.001f) continue;
            influences.Add(new KeyValuePair<int, float>(curveIndex, curveValue));
        }

        writer.WriteArray(influences, (writer, kvp) =>
        {
            writer.Write(kvp.Key);
            writer.Write(kvp.Value);
        });
    }
}
