using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.PCG;

public struct FPCGDataPtrWrapper(FAssetArchive Ar) : IUStruct
{
    public FPackageIndex Data = new(Ar);
}
