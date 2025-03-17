using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.RigVM;

public struct FRigVMGraphFunctionArgument
{
    public FName Name;
    public FName DisplayName;
    public FName CPPType;
    public FSoftObjectPath CPPTypeObject;
    public bool bIsArray;
    public ERigVMPinDirection Direction;
    public string DefaultValue;
    public bool bIsConst;
    public Dictionary<string, FText> PathToTooltip;

    public FRigVMGraphFunctionArgument(FAssetArchive Ar)
    {
        Name = Ar.ReadFName();
        DisplayName = Ar.ReadFName();
        CPPType = Ar.ReadFName();
        CPPTypeObject = new FSoftObjectPath(Ar);
        bIsArray = Ar.ReadBoolean();
        Direction = Ar.Read<ERigVMPinDirection>();
        DefaultValue = Ar.ReadFString();
        bIsConst = Ar.ReadBoolean();
        PathToTooltip = Ar.ReadMap(Ar.ReadFString, () => new FText(Ar));
    }
}
