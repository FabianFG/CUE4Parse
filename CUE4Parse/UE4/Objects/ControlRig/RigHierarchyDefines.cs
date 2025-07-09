using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Objects.ControlRig;

public struct FRigElementKey(FArchive Ar)
{
    public ERigElementType Type = EnumUtils.GetValueByName<ERigElementType>(Ar.ReadFName().Text);
    public FName Name = Ar.ReadFName();
}
