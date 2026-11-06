using System.Numerics.Tensors;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh;

public class FRawIndexBuffer
{
    public uint[]? Buffer { get; private set; }

    internal void SetIndices(ReadOnlySpan<ushort> indices)
    {
        Buffer = new uint[indices.Length];
        TensorPrimitives.ConvertTruncating(indices, Buffer);
    }

    internal void SetIndices(uint[] indices)
    {
        Buffer = indices;
    }

    public int Length
    {
        get
        {
            if (Buffer is not null)
                return Buffer.Length;
            throw new NullReferenceException("Indices have not been initialized. Call SetIndices first.");
        }
    }

    public int this[int i]
    {
        get
        {
            if (Buffer is not null)
                return (int)Buffer[i];
            throw new NullReferenceException("Indices have not been initialized. Call SetIndices first.");
        }
    }

    public int this[long i]
    {
        get
        {
            if (Buffer is not null)
                return (int)Buffer[i];
            throw new NullReferenceException("Indices have not been initialized. Call SetIndices first.");
        }
    }
}

