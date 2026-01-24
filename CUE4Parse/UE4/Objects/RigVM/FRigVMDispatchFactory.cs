using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.RigVM;

// just for serialization, we don't need to create actual factory from type and arguments
public class FRigVMDispatchFactory
{
    public FRigVMTemplateArgumentInfo[] FlattenedArguments;

    public FRigVMDispatchFactory(FAssetArchive Ar)
    {
        FlattenedArguments = Ar.ReadArray(() => new FRigVMTemplateArgumentInfo(Ar));
    }
}
