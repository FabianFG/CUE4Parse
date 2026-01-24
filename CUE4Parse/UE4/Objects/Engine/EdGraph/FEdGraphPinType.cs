using System;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Engine.EdGraph;

public class FEdGraphPinType : IUStruct
{
    public FName PinCategory;
    public FName PinSubCategory;
    public FPackageIndex PinSubCategoryObject;
    public FEdGraphTerminalType? PinValueType;
    public EPinContainerType ContainerType;
    public FSimpleMemberReference? PinSubCategoryMemberReference;

    public bool bIsReference;
    public bool bIsConst;
    public bool bIsWeakPointer;
    public bool bIsUObjectWrapper;
    public bool bSerializeAsSinglePrecisionFloat;

    private static readonly string[] WrappedCategories = ["class", "object", "interface", "softclass", "softobject"];

    public FEdGraphPinType() { }
    public FEdGraphPinType(FAssetArchive Ar)
    {
        if (FFrameworkObjectVersion.Get(Ar) >= FFrameworkObjectVersion.Type.PinsStoreFName)
        {
            PinCategory = Ar.ReadFName();
            PinSubCategory = Ar.ReadFName();
        }
        else
        {
            PinCategory = Ar.ReadFString();
            PinSubCategory = Ar.ReadFString();
        }

        if (Ar.Ver < EUnrealEngineObjectUE4Version.ADDED_SOFT_OBJECT_PATH)
        {
            // Fixup has to be here instead of in BP code because this is embedded in other structures
            if (PinCategory.Text.Equals("asset", StringComparison.OrdinalIgnoreCase))
            {
                PinCategory = "softobject";
            }
            else if (PinCategory.Text.Equals("assetclass", StringComparison.OrdinalIgnoreCase))
            {
                PinCategory = "softclass";
            }
        }

        //if(!Ar.IsObjectReferenceCollector() || Ar.IsModifyingWeakAndStrongReferences() || Ar.IsPersistent())
        PinSubCategoryObject = new FPackageIndex(Ar);

        if (FFrameworkObjectVersion.Get(Ar) >= FFrameworkObjectVersion.Type.EdGraphPinContainerType)
        {
            ContainerType = Ar.Read<EPinContainerType>();
            if (ContainerType == EPinContainerType.Map)
            {
                PinValueType = new FEdGraphTerminalType(Ar);
            }
        }
        else
        {
            bool bIsMap = false;
            bool bIsSet = false;
            bool bIsArray = false;

            if (FBlueprintsObjectVersion.Get(Ar) >= FBlueprintsObjectVersion.Type.AdvancedContainerSupport)
            {
                bIsMap = Ar.ReadBoolean();
                if (bIsMap)
                {
                    PinValueType = new FEdGraphTerminalType(Ar);
                }
                bIsSet = Ar.ReadBoolean();
            }

            bIsArray = Ar.ReadBoolean();

            ContainerType = EPinContainerType.None;
            if (bIsArray)
            {
                ContainerType = EPinContainerType.Array;
            }
            else if (bIsSet)
            {
                ContainerType = EPinContainerType.Set;
            }
            else if (bIsMap)
            {
                ContainerType = EPinContainerType.Map;
            }
        }

        bIsReference = Ar.ReadBoolean();
        bIsWeakPointer = Ar.ReadBoolean();

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.MEMBERREFERENCE_IN_PINTYPE)
        {
            PinSubCategoryMemberReference = new FSimpleMemberReference(Ar);
        }

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.SERIALIZE_PINTYPE_CONST)
        {
            bIsConst = Ar.ReadBoolean();
        }

        if (FReleaseObjectVersion.Get(Ar) >= FReleaseObjectVersion.Type.PinTypeIncludesUObjectWrapperFlag)
        {
            bIsUObjectWrapper = Ar.ReadBoolean();
        }

        if (bIsUObjectWrapper && !WrappedCategories.Contains(PinCategory.Text))
        {
            bIsUObjectWrapper = false;
        }

        bool bFixupPinCategories = FUE5ReleaseStreamObjectVersion.Get(Ar) < FUE5ReleaseStreamObjectVersion.Type.BlueprintPinsUseRealNumbers &&
                                   PinCategory.Text is "double" or "float";

        if (bFixupPinCategories)
        {
            PinCategory = "real";
            PinSubCategory = "double";
        }

        if (!Ar.IsFilterEditorOnly)
        {
            if (FUE5ReleaseStreamObjectVersion.Get(Ar) >= FUE5ReleaseStreamObjectVersion.Type.SerializeFloatPinDefaultValuesAsSinglePrecision)
            {
                bSerializeAsSinglePrecisionFloat = Ar.ReadBoolean();
            }
            else if (PinCategory.Text == "float")
            {
                bSerializeAsSinglePrecisionFloat = true;
            }
        }
    }
}
