using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse_Conversion.Writers.ActorX.Structs.Animations;
using CUE4Parse.UE4.Assets.Exports.Rig;
using CUE4Parse.UE4.Assets.Exports.Rig.RigLogic;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.DNA;

public static class DNAConverter
{
    public static bool TryConvert(this IDnaAsset dna, out CPoseAsset convertedPoseAsset)
    {
        if (dna.Snapshot != null)
            return TryConvertSnapshot(dna, out convertedPoseAsset);
        return TryConvertRaw(dna, out convertedPoseAsset);
    }

    private static bool TryConvertRaw(IDnaAsset dna, out CPoseAsset convertedPoseAsset)
    {
        convertedPoseAsset = new CPoseAsset();
        convertedPoseAsset.CurveNames = [..dna.GetRawControlNames().Select(name => name.Replace(".", "_"))];
        var rotation = GetRotationConvention(dna);

        for (var i = 0; i < dna.GetRawControlCount(); i++)
        {
            var controlName = dna.GetRawControlName(i).Replace(".", "_");
            CPoseData poseData = new()
            {
                PoseName = controlName,
                CurveData = []
            };
            var jointOutputs = CalculateForControlIndex(dna, i);

            var n = 0;
            for (var jointIndex = 0; jointIndex < dna.GetJointCount(); ++jointIndex)
            {
                AddEulerPoseKey(poseData, dna.GetJointName(jointIndex), jointOutputs, n, rotation);
                n += 9;
            }

            convertedPoseAsset.Poses.Add(poseData);
        }

        return convertedPoseAsset.Poses.Count > 0 || dna.GetRawControlCount() == 0;
    }

    private static bool TryConvertSnapshot(IDnaAsset dna, out CPoseAsset convertedPoseAsset)
    {
        var snapshot = dna.Snapshot!;
        convertedPoseAsset = new CPoseAsset();
        var controlNames = dna.GetRawControlNames();
        if (controlNames.Length == 0)
        {
            controlNames = Enumerable.Range(0, snapshot.Metadata.RawControlCount)
                .Select(i => $"RawControl_{i}")
                .ToArray();
        }

        convertedPoseAsset.CurveNames = [..controlNames.Select(name => name.Replace(".", "_"))];
        var jointNames = dna.GetJointNames();
        var attrsPerJoint = snapshot.Configuration.AttributesPerJoint;
        var jointCount = attrsPerJoint == 0 ? 0 : snapshot.Metadata.JointAttributeCount / attrsPerJoint;
        if (jointNames.Length == 0)
            jointNames = Enumerable.Range(0, jointCount).Select(i => $"Joint_{i}").ToArray();

        var snapshotRotation = ((ERotationSequence) snapshot.Metadata.RotationSequence,
            NormalizeSign(snapshot.Metadata.RotationSignX),
            NormalizeSign(snapshot.Metadata.RotationSignY),
            NormalizeSign(snapshot.Metadata.RotationSignZ));

        for (var i = 0; i < controlNames.Length; i++)
        {
            var poseData = new CPoseData
            {
                PoseName = controlNames[i].Replace(".", "_"),
                CurveData = []
            };
            var outputs = RigLogicEvaluator.EvaluateJointsForRawControl(snapshot, i);
            var n = 0;
            for (var jointIndex = 0; jointIndex < jointCount && jointIndex < jointNames.Length; jointIndex++)
            {
                if (snapshot.Configuration.RotationType == RlRotationType.Quaternions)
                {
                    AddQuatPoseKey(poseData, jointNames[jointIndex], outputs, n);
                    n += attrsPerJoint;
                }
                else
                {
                    AddEulerPoseKey(poseData, jointNames[jointIndex], outputs, n, snapshotRotation);
                    n += 9;
                }
            }

            convertedPoseAsset.Poses.Add(poseData);
        }

        return convertedPoseAsset.Poses.Count > 0;
    }

    /// <summary>
    /// Rotation convention the DNA's euler angles are authored in, from the extended descriptor.
    /// </summary>
    private static (ERotationSequence Sequence, int SignX, int SignY, int SignZ) GetRotationConvention(IDnaAsset dna)
    {
        if (dna.Layers != null && DnaAssetQueries.TryGet(dna.Layers, "dsce", out RawDescriptorExt ext))
            return (ext.RotationSequence, ToSign(ext.RotationSign.X), ToSign(ext.RotationSign.Y), ToSign(ext.RotationSign.Z));

        return (ERotationSequence.XYZ, 1, 1, 1);
    }

    private static int ToSign(ERotationDirection direction) => direction == ERotationDirection.Negative ? -1 : 1;

    private static int NormalizeSign(int sign) => sign < 0 ? -1 : 1;

    // Joint deltas are already in the space consumers expect, so translation and rotation both pass
    // through as authored: a positive value on the first, second and third translation component
    // moves the joint left, down and forward respectively.
    private static void AddEulerPoseKey(CPoseData poseData, string jointName, IReadOnlyList<float> jointOutputs, int n,
        (ERotationSequence Sequence, int SignX, int SignY, int SignZ) rotation)
    {
        if (n + 8 >= jointOutputs.Count)
            return;

        var (qx, qy, qz, qw) = RigLogicEvaluator.EulerDegreesToQuat(
            jointOutputs[n + 3], jointOutputs[n + 4], jointOutputs[n + 5],
            rotation.Sequence, rotation.SignX, rotation.SignY, rotation.SignZ);

        CPoseKey key = new
        (
            jointName,
            new FVector(jointOutputs[n], jointOutputs[n + 1], jointOutputs[n + 2]),
            new FQuat(qx, qy, qz, qw),
            new FVector(jointOutputs[n + 6], jointOutputs[n + 7], jointOutputs[n + 8])
        );

        if (!key.Location.IsZero() || !(key.Rotation.IsIdentity() || key.Rotation.IsVectorZero()) || !key.Scale.IsZero())
            poseData.Keys.Add(key);
    }

    private static void AddQuatPoseKey(CPoseData poseData, string jointName, IReadOnlyList<float> jointOutputs, int n)
    {
        if (n + 9 >= jointOutputs.Count)
            return;
        var rotation = new FQuat(jointOutputs[n + 3], jointOutputs[n + 4], jointOutputs[n + 5], jointOutputs[n + 6]);
        CPoseKey key = new
        (
            jointName,
            new FVector(jointOutputs[n], jointOutputs[n + 1], jointOutputs[n + 2]),
            rotation,
            new FVector(jointOutputs[n + 7], jointOutputs[n + 8], jointOutputs[n + 9])
        );

        if (!key.Location.IsZero() || !(key.Rotation.IsIdentity() || key.Rotation.IsVectorZero()) || !key.Scale.IsZero())
            poseData.Keys.Add(key);
    }

    private static List<float> CalculateForControlIndex(IDnaAsset dna, int activeInputIndex)
    {
        var outputs = new float[dna.GetJointCount() * 9];
        var jointGroups = dna.GetBehavior()?.Joints.JointGroups;
        if (jointGroups == null) return outputs.ToList();

        foreach (var jointGroup in jointGroups)
            ProcessJointGroup(jointGroup, activeInputIndex, outputs);

        return outputs.ToList();
    }

    private static void ProcessJointGroup(RawJointGroup jointGroup, int controlIndex, float[] outputs)
    {
        var outputCount = jointGroup.LODs[0];
        if (outputCount == 0) return;

        var offset = Array.IndexOf(jointGroup.InputIndices, (ushort) controlIndex);
        if (offset == -1) return;

        var values = jointGroup.Values;
        for (var outIdx = 0; outIdx < outputCount && outIdx < jointGroup.OutputIndices.Length; outIdx++)
        {
            var valueIndex = offset + (outIdx * jointGroup.InputIndices.Length);
            if (valueIndex >= values.Length) continue;
            var outputIndex = jointGroup.OutputIndices[outIdx];
            outputs[outputIndex] = values[valueIndex];
        }
    }
}
