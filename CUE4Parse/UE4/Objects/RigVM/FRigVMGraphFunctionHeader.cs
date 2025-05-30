using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.RigVM;

public class FRigVMGraphFunctionHeader
{
    public FRigVMGraphFunctionIdentifier LibraryPointer;
    public FRigVMVariant? Variant;
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
    public FRigVMNodeLayout Layout;

    public FRigVMGraphFunctionHeader(FAssetArchive Ar)
    {
        LibraryPointer = new FRigVMGraphFunctionIdentifier(Ar);
        if (FRigVMObjectVersion.Get(Ar) >= FRigVMObjectVersion.Type.AddVariantToFunctionIdentifier)
            Variant = new FRigVMVariant(Ar);
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
        Dependencies = Ar.ReadMap(() => new FRigVMGraphFunctionIdentifier(Ar), Ar.Read<uint>);
        ExternalVariables = Ar.ReadArray(() => new FRigVMExternalVariable(Ar));

        if (FRigVMObjectVersion.Get(Ar) >= FRigVMObjectVersion.Type.FunctionHeaderStoresLayout)
        {
            Layout = new FRigVMNodeLayout(Ar);
        }
    }
}

public class FRigVMVariant(FAssetArchive Ar)
{
    public FGuid Guid = Ar.Read<FGuid>();
    public FRigVMTag[] Tags = Ar.ReadArray(() => new FRigVMTag(Ar));
}

public class FRigVMTag(FAssetArchive Ar)
{
    public FName Name = Ar.ReadFName();
    public string Label = Ar.ReadFString();
    public FText ToolTip = new FText(Ar);
    public FLinearColor Color = Ar.Read<FLinearColor>();
    public bool bShowInUserInterface = Ar.ReadBoolean();
    public bool bMarksSubjectAsInvalid = Ar.ReadBoolean();
}
