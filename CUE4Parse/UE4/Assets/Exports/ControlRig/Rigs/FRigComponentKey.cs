using CUE4Parse.UE4.Objects.ControlRig;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.ControlRig.Rigs;

public struct FRigComponentKey
{
    public FName Name;
    public FRigElementKey ElementKey;

    public FRigComponentKey(FArchive Ar)
    {
        Name = Ar.ReadFName();
        ElementKey = new FRigElementKey(Ar);
    }
}
