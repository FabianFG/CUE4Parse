using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.RigVM;

public enum ERigVMFunctionArgumentDirection
{
    Input, // A const input value
    Output, // A mutable output value
    Invalid // The max value for this enum - used for guarding.
}

public class FRigVMFunctionArgument
{
    public string LocalNameString;
    public string LocalTypeString;
    public ERigVMFunctionArgumentDirection DirectionAsInt32;

    public FRigVMFunctionArgument(FAssetArchive Ar)
    {
        LocalNameString = Ar.ReadFString();
        LocalTypeString = Ar.ReadFString();
        DirectionAsInt32 = Ar.Read<ERigVMFunctionArgumentDirection>();
    }
}

public class FRigVMFunction
{
    public string Name;
    public FPackageIndex Struct;
    public FPackageIndex FactoryStruct;
    public FRigVMFunctionArgument[] Arguments;

    public FRigVMFunction(FAssetArchive Ar)
    {
        Name = Ar.ReadFString();
        Struct = new FPackageIndex(Ar);
        FactoryStruct = new FPackageIndex(Ar);
        Arguments = Ar.ReadArray(() => new FRigVMFunctionArgument(Ar));
    }
}
