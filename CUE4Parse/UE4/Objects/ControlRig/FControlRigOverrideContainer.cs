using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.ControlRig;

public class FControlRigOverrideContainer : IUStruct
{
    public bool bUsesKeyForSubject;
    public FControlRigOverrideValue[] Values;

    public FControlRigOverrideContainer()
    {
        bUsesKeyForSubject = false;
        Values = [];
    }

    public FControlRigOverrideContainer(FAssetArchive Ar)
    {
        bUsesKeyForSubject = Ar.ReadBoolean();
        // bool bStoresOnlyPathAndLeafProperty = FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.OverridesStorePathAndLeafPropertyOnly;
        Values = Ar.ReadArray(() => new FControlRigOverrideValue(Ar));
    }
}
