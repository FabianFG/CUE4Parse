using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions;

// Custom serialization version for changes made in Dev-Blueprints stream
public static class FBlueprintsObjectVersion
{
    public enum Type
    {
        // Before any version changes were made
        BeforeCustomVersionWasAdded = 0,
        OverridenEventReferenceFixup,
        CleanBlueprintFunctionFlags,
        ArrayGetByRefUpgrade,
        EdGraphPinOptimized,
        AllowDeletionConformed,
        AdvancedContainerSupport,
        SCSHasComponentTemplateClass,
        ComponentTemplateClassSupport,
        ArrayGetFuncsReplacedByCustomNode,
        DisallowObjectConfigVars,

        // -----<new versions can be added above this line>-------------------------------------------------
        VersionPlusOne,
        LatestVersion = VersionPlusOne - 1
    }

    public static readonly FGuid GUID = new(0xB0D832E4, 0x1F894F0D, 0xACCF7EB7, 0x36FD4AA2);

    public static Type Get(FArchive Ar)
    {
        var ver = Ar.CustomVer(GUID);
        if (ver >= 0)
            return (Type) ver;

        return Ar.Game switch
        {
            _ => Type.LatestVersion
        };
    }
}
