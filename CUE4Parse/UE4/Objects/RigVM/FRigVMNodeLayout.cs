using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.RigVM;

public struct FRigVMPinCategory
{
    public string Path;
    public string[] Elements;
    public bool bExpandedByDefault;

    public FRigVMPinCategory(FAssetArchive Ar)
    {
        Path = Ar.ReadFString();
        Elements = Ar.ReadArray(Ar.ReadFString);

        if (FRigVMObjectVersion.Get(Ar) < FRigVMObjectVersion.Type.FunctionHeaderLayoutStoresCategoryExpansion)
        {
            bExpandedByDefault = true;
        }
        else
        {
            bExpandedByDefault = Ar.ReadBoolean();
        }
    }
}

public struct FRigVMNodeLayout
{
    public FRigVMPinCategory[] Categories;
    public Dictionary<string, int> PinIndexInCategory;
    public Dictionary<string, string> DisplayNames;

    public FRigVMNodeLayout(FAssetArchive Ar)
    {
        Categories = Ar.ReadArray(() => new FRigVMPinCategory(Ar));
        if (FRigVMObjectVersion.Get(Ar) < FRigVMObjectVersion.Type.FunctionHeaderLayoutStoresPinIndexInCategory)
        {
            PinIndexInCategory = [];
        }
        else
        {
            PinIndexInCategory = Ar.ReadMap(Ar.ReadFString, Ar.Read<int>);
        }
        DisplayNames = Ar.ReadMap(Ar.ReadFString, Ar.ReadFString);
    }
}
