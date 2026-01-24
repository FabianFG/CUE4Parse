using System;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Buffers;

public class FMeshBufferSet
{
    public uint ElementCount;
    public FMeshBuffer[] Buffers;
    public EMeshBufferSetFlags Flags;
    
    public FMeshBufferSet(FMutableArchive Ar)
    {
        ElementCount = Ar.Read<uint>();
        Buffers = Ar.ReadArray(() => new FMeshBuffer(Ar));
        Flags = Ar.Read<EMeshBufferSetFlags>();
    }
}

[Flags]
public enum EMeshBufferSetFlags : uint
{
    None = 0,
    IsDescriptor = 1 << 0
}