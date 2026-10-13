using CUE4Parse.UE4.Assets.Exports.Rig;

namespace CUE4Parse.UE4.Assets.Exports.Rig.RigLogic;

public static class RigLogicEvaluator
{
    private const int BlockHeight = 8;
    private const int HalfBlockHeight = 4;

    public static float[] EvaluateJointsForRawControl(RigLogicSnapshot snapshot, int rawControlIndex, ushort lod = 0)
    {
        var meta = snapshot.Metadata;
        var inputs = new float[meta.RawControlCount + meta.PsdControlCount + meta.MlControlCount + meta.RbfControlCount];
        foreach (var init in snapshot.Controls.InitialValues)
        {
            if (init.Index < inputs.Length)
                inputs[init.Index] = init.Value;
        }

        if ((uint) rawControlIndex < meta.RawControlCount)
            inputs[rawControlIndex] = 1f;

        if (snapshot.PsdNet != null)
            CalculatePsd(snapshot.PsdNet, inputs, lod);

        var outputs = new float[meta.JointAttributeCount];
        if (snapshot.Configuration.RotationType == RlRotationType.Quaternions)
        {
            var stride = snapshot.Configuration.AttributesPerJoint;
            for (var i = 3; i < outputs.Length; i += stride)
                outputs[i + 3] = 1f;
        }

        if (snapshot.Joints.Bpcm.JointGroups.Length > 0)
            CalculateBpcm(snapshot.Joints.Bpcm, inputs, outputs, lod);

        if (snapshot.Configuration.RotationType == RlRotationType.Quaternions)
            ConvertEulerDeltasToQuaternions(snapshot, outputs, lod);

        return outputs;
    }

    private static void CalculatePsd(RlPsdNet net, float[] inputs, ushort lod)
    {
        if (lod >= net.InputLods.Length || lod >= net.OutputLods.Length)
            return;

        var clamp = new float[inputs.Length];
        foreach (var inputIndex in net.InputLods[lod])
        {
            if (inputIndex < inputs.Length)
                clamp[inputIndex] = Math.Clamp(inputs[inputIndex], 0f, 1f);
        }

        foreach (var outputIndex in net.OutputLods[lod])
        {
            var psdIndex = outputIndex - net.PsdMinIndex;
            if (psdIndex < 0 || psdIndex >= net.Psds.Length)
                continue;
            var psd = net.Psds[psdIndex];
            var value = psd.Weight;
            var end = psd.Offset + psd.Size;
            for (var i = psd.Offset; i < end && i < (ulong) net.InputIndicesPerPsd.Length; i++)
            {
                var idx = net.InputIndicesPerPsd[i];
                value *= idx < clamp.Length ? clamp[idx] : 0f;
            }

            if (outputIndex < inputs.Length)
                inputs[outputIndex] = Math.Min(1f, value);
        }
    }

    private static void CalculateBpcm(RlBpcmStorage storage, float[] inputs, float[] outputs, ushort lod)
    {
        foreach (var group in storage.JointGroups)
        {
            if (group.RowCount == 0)
                continue;
            ProcessJointGroup(storage, group, inputs, outputs, lod);
        }
    }

    private static void ProcessJointGroup(RlBpcmStorage storage, RlBpcmJointGroup group, float[] inputs, float[] outputs, ushort lod)
    {
        var lodIndex = group.LodsOffset + lod;
        if (lodIndex >= storage.LodRegions.Length)
            return;
        var region = storage.LodRegions[lodIndex];
        var values = storage.Values.AsSpan((int) group.ValuesOffset);
        var inputIndices = storage.InputIndices.AsSpan((int) group.InputIndicesOffset);
        var outputIndices = storage.OutputIndices.AsSpan((int) group.OutputIndicesOffset);

        var inputCount = (int) region.InputLods.Size;
        var inputEnd4 = (int) region.InputLods.SizeAlignedTo4;
        var inputEnd8 = (int) region.InputLods.SizeAlignedTo8;
        var outputCount = (int) region.OutputLods.Size;
        var paddedLast = (int) region.OutputLods.SizePaddedToLastFullBlock;
        var paddedSecond = (int) region.OutputLods.SizePaddedToSecondLastFullBlock;
        var colCount = (int) group.ColCount;
        var valueOffset = 0;
        var outCursor = 0;

        for (; outCursor < paddedSecond; outCursor += BlockHeight, valueOffset += colCount * BlockHeight)
        {
            Span<float> outbuf = stackalloc float[BlockHeight];
            ProcessBlocks8x4(inputIndices, inputEnd4, inputCount, inputs, values[valueOffset..], outbuf);
            for (var i = 0; i < BlockHeight; i++)
                outputs[outputIndices[outCursor + i]] = outbuf[i];
        }

        for (; outCursor < paddedLast; outCursor += BlockHeight, valueOffset += colCount * BlockHeight)
        {
            Span<float> outbuf = stackalloc float[BlockHeight];
            ProcessBlocks8x4(inputIndices, inputEnd4, inputCount, inputs, values[valueOffset..], outbuf);
            var keep = outputCount % BlockHeight;
            for (var i = 0; i < keep; i++)
                outputs[outputIndices[outCursor + i]] = outbuf[i];
        }

        for (; outCursor < outputCount; outCursor += HalfBlockHeight, valueOffset += colCount * HalfBlockHeight)
        {
            Span<float> outbuf = stackalloc float[HalfBlockHeight];
            ProcessBlocks4x8(inputIndices, inputEnd8, inputCount, inputs, values[valueOffset..], outbuf);
            var keep = Math.Min(HalfBlockHeight, outputCount - outCursor);
            for (var i = 0; i < keep; i++)
                outputs[outputIndices[outCursor + i]] = outbuf[i];
        }
    }

    private static void ProcessBlocks8x4(ReadOnlySpan<ushort> inputIndices, int endAlignedTo4, int inputEnd, float[] inputs,
        ReadOnlySpan<float> values, Span<float> outbuf)
    {
        Span<float> sum = stackalloc float[8];
        var valueIndex = 0;
        for (var i = 0; i < endAlignedTo4; i += 4)
        {
            for (var lane = 0; lane < 4; lane++)
            {
                var input = inputs[inputIndices[i + lane]];
                for (var row = 0; row < 8; row++)
                    sum[row] += values[valueIndex + lane * 8 + row] * input;
            }
            valueIndex += 32;
        }

        for (var i = endAlignedTo4; i < inputEnd; i++)
        {
            var input = inputs[inputIndices[i]];
            for (var row = 0; row < 8; row++)
                sum[row] += values[valueIndex++] * input;
        }

        sum.CopyTo(outbuf);
    }

    private static void ProcessBlocks4x8(ReadOnlySpan<ushort> inputIndices, int endAlignedTo8, int inputEnd, float[] inputs,
        ReadOnlySpan<float> values, Span<float> outbuf)
    {
        Span<float> sum = stackalloc float[4];
        var valueIndex = 0;
        for (var i = 0; i < endAlignedTo8; i += 8)
        {
            for (var lane = 0; lane < 8; lane++)
            {
                var input = inputs[inputIndices[i + lane]];
                for (var row = 0; row < 4; row++)
                    sum[row] += values[valueIndex + lane * 4 + row] * input;
            }
            valueIndex += 32;
        }

        for (var i = endAlignedTo8; i < inputEnd; i++)
        {
            var input = inputs[inputIndices[i]];
            for (var row = 0; row < 4; row++)
                sum[row] += values[valueIndex++] * input;
        }

        sum.CopyTo(outbuf);
    }

    private static void ConvertEulerDeltasToQuaternions(RigLogicSnapshot snapshot, float[] outputs, ushort lod)
    {
        var storage = snapshot.Joints.Bpcm;
        var seq = (ERotationSequence) snapshot.Metadata.RotationSequence;
        var sx = snapshot.Metadata.RotationSignX;
        var sy = snapshot.Metadata.RotationSignY;
        var sz = snapshot.Metadata.RotationSignZ;
        foreach (var group in storage.JointGroups)
        {
            if (group.RowCount == 0)
                continue;
            var rotLodIndex = group.OutputRotationLodsOffset + lod;
            if (rotLodIndex >= storage.OutputRotationLods.Length)
                continue;
            var rotCount = storage.OutputRotationLods[rotLodIndex];
            var rotBase = (int) group.OutputRotationIndicesOffset;
            for (var row = 0; row < rotCount; row++)
            {
                var start = storage.OutputRotationIndices[rotBase + row];
                var q = EulerDegreesToQuat(outputs[start], outputs[start + 1], outputs[start + 2], seq, sx, sy, sz);
                outputs[start] = q.x;
                outputs[start + 1] = q.y;
                outputs[start + 2] = q.z;
                outputs[start + 3] = q.w;
            }
        }
    }

    public static (float x, float y, float z, float w) EulerDegreesToQuat(float xDeg, float yDeg, float zDeg,
        ERotationSequence sequence, int signX, int signY, int signZ)
    {
        const float degToRad = MathF.PI / 180f;
        var x = xDeg * signX * degToRad * 0.5f;
        var y = yDeg * signY * degToRad * 0.5f;
        var z = zDeg * signZ * degToRad * 0.5f;
        var sx = MathF.Sin(x); var cx = MathF.Cos(x);
        var sy = MathF.Sin(y); var cy = MathF.Cos(y);
        var sz = MathF.Sin(z); var cz = MathF.Cos(z);

        return sequence switch
        {
            ERotationSequence.XZY => (
                sx * cy * cz + cx * sy * sz,
                cx * sy * cz + sx * cy * sz,
                cx * cy * sz - sx * sy * cz,
                cx * cy * cz - sx * sy * sz),
            ERotationSequence.YXZ => (
                sx * cy * cz - cx * sy * sz,
                cx * sy * cz + sx * cy * sz,
                cx * cy * sz + sx * sy * cz,
                cx * cy * cz - sx * sy * sz),
            _ => (
                sx * cy * cz - cx * sy * sz,
                cx * sy * cz + sx * cy * sz,
                cx * cy * sz - sx * sy * cz,
                cx * cy * cz + sx * sy * sz)
        };
    }
}
