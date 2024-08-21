using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.RigVM;

public struct FRigVMGraphFunctionHeader
{
    public FRigVMGraphFunctionIdentifier LibraryPointer;
    public FName Name;
    public string NodeTitle;
    public FLinearColor NodeColor;
    public FText? Tooltip_DEPRECATED;
    public string? Description;
    public string Category;
    public string Keywords;
    public FRigVMGraphFunctionArgument[] Arguments = [];
    public Dictionary<FRigVMGraphFunctionIdentifier, uint> Dependencies = [];
    public FRigVMExternalVariable[] ExternalVariables = [];

    public FRigVMGraphFunctionHeader(FAssetArchive Ar)
    {
        LibraryPointer = new FRigVMGraphFunctionIdentifier(Ar);
        Name = Ar.ReadFName();
        NodeTitle = Ar.ReadFString();
        NodeColor = Ar.Read<FLinearColor>();

        if (FRigVMObjectVersion.Get(Ar) < FRigVMObjectVersion.Type.VMRemoveTooltipFromFunctionHeader)
        {
            Tooltip_DEPRECATED = new FText(Ar);
        }
        else
        {
            Description = Ar.ReadFString();
        }

        Category = Ar.ReadFString();
        Keywords = Ar.ReadFString();
        Arguments = Ar.ReadArray(() => new FRigVMGraphFunctionArgument(Ar));
        var num = Ar.Read<int>();
        for (var i = 0; i < num; i++)
        {
            Dependencies[new FRigVMGraphFunctionIdentifier(Ar)] = Ar.Read<uint>();
        }
        ExternalVariables = Ar.ReadArray(() => new FRigVMExternalVariable(Ar));
    }
}
