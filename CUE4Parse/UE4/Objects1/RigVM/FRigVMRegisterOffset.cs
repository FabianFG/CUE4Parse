using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.RigVM
{
    public class FRigVMRegisterOffset
    {
        public readonly int[] Segments;
        public readonly ERigVMRegisterType Type;
        public readonly FName CPPType;
        public readonly FPackageIndex ScriptStruct; // UScriptStruct
        public readonly ushort ElementSize;
        public readonly FPackageIndex ParentScriptStruct; // UScriptStruct
        public readonly string CachedSegmentPath;
        public readonly int ArrayIndex;

        public FRigVMRegisterOffset(FAssetArchive Ar)
        {
            Segments = Ar.ReadArray<int>();
            Type = Ar.Read<ERigVMRegisterType>();
            CPPType = Ar.ReadFName();
            ScriptStruct = new FPackageIndex(Ar);
            ElementSize = Ar.Read<ushort>();
            ParentScriptStruct = new FPackageIndex(Ar);
            CachedSegmentPath = Ar.ReadFString();
            ArrayIndex = Ar.Read<int>();
        }
    }
}