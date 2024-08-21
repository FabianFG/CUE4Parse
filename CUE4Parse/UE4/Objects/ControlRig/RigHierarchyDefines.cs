using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Objects.ControlRig;

public struct FRigElementKey(FAssetArchive Ar)
{
    public ERigElementType Type = EnumUtils.GetValueByName<ERigElementType>(Ar.ReadFName().Text);
    public FName Name = Ar.ReadFName();
}
