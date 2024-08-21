using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.RigVM;

public struct FRigVMFunctionCompilationPropertyPath(FAssetArchive Ar)
{
    public int PropertyIndex = Ar.Read<int>();
    public string HeadCPPType = Ar.ReadFString();
    public string SegmentPath = Ar.ReadFString();
}
