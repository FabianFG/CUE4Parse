using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.RigVM;

public class FRigVMRegister
{
    public readonly ERigVMRegisterType Type;
    public readonly uint ByteIndex;
    public readonly ushort ElementSize;
    public readonly ushort ElementCount;
    public readonly ushort SliceIndex;
    public readonly ushort SliceCount;
    public readonly byte AlignmentBytes;
    public readonly ushort TrailingBytes;
    public readonly FName Name;
    public readonly int ScriptStructIndex;
    public readonly bool bIsArray;
    public readonly bool bIsDynamic;
    public object? View;

    public FRigVMRegister(FArchive Ar, FRigVMMemoryLayout layout)
    {
        Type = Ar.Read<ERigVMRegisterType>();
        ByteIndex = Ar.Read<uint>();
        ElementSize = Ar.Read<ushort>();
        ElementCount = Ar.Read<ushort>();
        SliceIndex = Ar.Read<ushort>();
        SliceCount = Ar.Read<ushort>();
        AlignmentBytes = Ar.Read<byte>();
        TrailingBytes = Ar.Read<ushort>();
        Name = Ar.ReadFName();
        ScriptStructIndex = Ar.Read<int>();
        // FRigVMRegister::Serialize - 4.26 snapshots can have bIsArray but not bIsDynamic
        bIsArray = layout.bSerializeRegisterArrayState && Ar.ReadBoolean();
        bIsDynamic = layout.bSerializeRegisterDynamicState && Ar.ReadBoolean();
    }

    public bool IsDynamic() => bIsDynamic;
    public bool IsNestedDynamic() => bIsDynamic && bIsArray;
    public ulong GetWorkByteIndex(int sliceIndex = 0) => (ulong) (ByteIndex + sliceIndex * ElementCount * ElementSize);
}
