using CUE4Parse_Conversion.PoseAsset.Conversion;
using CUE4Parse_Conversion.UEFormat.Natives;

namespace CUE4Parse_Conversion.PoseAsset.UEFormat;

public static class UEPose
{
    public static byte[] Export(string name, string objectPath, CPoseAsset poseAsset, ExporterOptions options)
    {
        using var pin = new NativePinScope();

        var poses = new UEFormatPoseDataDesc[poseAsset.Poses.Count];
        for (var i = 0; i < poseAsset.Poses.Count; i++)
        {
            var pose = poseAsset.Poses[i];
            var keys = new UEFormatPoseKeyDesc[pose.Keys.Count];
            for (var k = 0; k < pose.Keys.Count; k++)
            {
                var key = pose.Keys[k];
                keys[k] = new UEFormatPoseKeyDesc
                {
                    BoneName = pin.AllocUtf8(key.BoneName),
                    Location = UEFormatNativeSave.ToVector(key.Location),
                    Rotation = UEFormatNativeSave.ToQuat(key.Rotation),
                    Scale = UEFormatNativeSave.ToVector(key.Scale),
                };
            }

            var influences = new List<UEFormatPoseCurveInfluenceDesc>();
            for (var curveIndex = 0; curveIndex < pose.CurveData.Length; curveIndex++)
            {
                var curveValue = pose.CurveData[curveIndex];
                if (Math.Abs(curveValue) < 0.001f) continue;
                influences.Add(new UEFormatPoseCurveInfluenceDesc
                {
                    CurveIndex = curveIndex,
                    Influence = curveValue,
                });
            }

            var influenceArray = influences.ToArray();
            poses[i] = new UEFormatPoseDataDesc
            {
                PoseName = pin.AllocUtf8(pose.PoseName),
                Keys = pin.PinArray(keys),
                KeyCount = keys.Length,
                Curves = pin.PinArray(influenceArray),
                CurveCount = influenceArray.Length,
            };
        }

        var curveNamePtrs = new IntPtr[poseAsset.CurveNames.Count];
        for (var i = 0; i < poseAsset.CurveNames.Count; i++)
            curveNamePtrs[i] = pin.AllocUtf8(poseAsset.CurveNames[i]);

        var desc = new UEFormatPoseDesc
        {
            Poses = pin.PinArray(poses),
            PoseCount = poses.Length,
            CurveNames = pin.PinArray(curveNamePtrs),
            CurveNameCount = curveNamePtrs.Length,
        };
        return UEFormatNativeSave.SavePose(ref desc, name, objectPath, options, pin);
    }
}
