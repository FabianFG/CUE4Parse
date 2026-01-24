using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Engine.EdGraph;

public class FEdGraphTerminalType
{
    public FName TerminalCategory;
    public FName TerminalSubCategory;
    public FPackageIndex TerminalSubCategoryObject;
    public FName TerminalSubCategoryMemberReference;
    public bool bIsConst;
    public bool bIsWeakPointer;
    public bool bIsUObjectWrapper;

    public FEdGraphTerminalType(FAssetArchive Ar)
    {
        if (FFrameworkObjectVersion.Get(Ar) >= FFrameworkObjectVersion.Type.PinsStoreFName)
        {
            TerminalCategory = Ar.ReadFName();
            TerminalSubCategory = Ar.ReadFName();
        }
        else
        {
            var TerminalCategoryStr = Ar.ReadFString();
            if (Ar.Ver < EUnrealEngineObjectUE4Version.ADDED_SOFT_OBJECT_PATH)
            {
                // Fixup has to be here instead of in BP code because this is embedded in other structures
                if (TerminalCategoryStr.Equals("asset", StringComparison.OrdinalIgnoreCase))
                {
                    TerminalCategoryStr = "softobject";
                }
                else if (TerminalCategoryStr.Equals("assetclass", StringComparison.OrdinalIgnoreCase))
                {
                    TerminalCategoryStr = "softclass";
                }
            }
            TerminalCategory = TerminalCategoryStr;
            TerminalSubCategory = Ar.ReadFString();
        }

        //if (!Ar.IsObjectReferenceCollector() || Ar.IsModifyingWeakAndStrongReferences() || Ar.IsPersistent())
        TerminalSubCategoryObject = new FPackageIndex(Ar);

        bIsConst = Ar.ReadBoolean();
        bIsWeakPointer = Ar.ReadBoolean();

        if (FReleaseObjectVersion.Get(Ar) >= FReleaseObjectVersion.Type.PinTypeIncludesUObjectWrapperFlag)
        {
            bIsUObjectWrapper = Ar.ReadBoolean();
        }

        bool bFixupPinCategories = FUE5ReleaseStreamObjectVersion.Get(Ar) < FUE5ReleaseStreamObjectVersion.Type.BlueprintPinsUseRealNumbers &&
                                   TerminalCategory.Text is "double" or "float";

        if (bFixupPinCategories)
        {
            TerminalCategory = "real";
            TerminalSubCategory = "double";
        }
    }
}
