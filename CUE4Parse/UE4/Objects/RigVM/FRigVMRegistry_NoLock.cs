using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.RigVM;

public struct FRigVMTemplateArgumentType(FAssetArchive Ar)
{
    public FName CPPType = Ar.ReadFName();
    public FPackageIndex CPPTypeObject = new FPackageIndex(Ar);
}

public class FRigVMRegistry_NoLock
{
    public FPackageIndex[] AllowedClasses;
    public FRigVMTemplateArgumentType[] Types;
    public Dictionary<FPackageIndex, FRigVMDispatchFactory> FactoryStructToFactory;
    public FRigVMFunction[] Functions;

    public FRigVMRegistry_NoLock(FAssetArchive Ar)
    {
        AllowedClasses = Ar.ReadArray(() => new FPackageIndex(Ar));
        Types = Ar.ReadArray(() => new FRigVMTemplateArgumentType(Ar));
        FactoryStructToFactory = Ar.ReadMap(() => new FPackageIndex(Ar), () => new FRigVMDispatchFactory(Ar));
        Functions = Ar.ReadArray(() => new FRigVMFunction(Ar));
    }
}
