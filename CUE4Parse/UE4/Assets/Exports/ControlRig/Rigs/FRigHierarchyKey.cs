using CUE4Parse.UE4.Objects.ControlRig;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.ControlRig.Rigs;

public struct FRigHierarchyKey
{
    public FRigElementKey? Element;
    public FRigComponentKey? Component;

    public FRigHierarchyKey(FArchive Ar)
    {
        var bIsElement = Ar.ReadBoolean();
        if (bIsElement)
        {
            Element = new FRigElementKey(Ar);
        }

        var bIsComponent = Ar.ReadBoolean();
        if (bIsComponent)
        {
            Component = new FRigComponentKey(Ar);
        }
    }
}
