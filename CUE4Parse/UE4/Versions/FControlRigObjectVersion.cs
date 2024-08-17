using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions;

// Custom serialization version for changes made in Dev-Anim stream
public static class FControlRigObjectVersion
{
    public enum Type
    {
        // Before any version changes were made
        BeforeCustomVersionWasAdded,

        // Added execution pins and removed hierarchy ref pins
        RemovalOfHierarchyRefPins,

        // Refactored operators to store FCachedPropertyPath instead of string
        OperatorsStoringPropertyPaths,

        // Introduced new RigVM as a backend
        SwitchedToRigVM,

        // Added a new transform as part of the control
        ControlOffsetTransform,

        // Using a cache data structure for key indices now
        RigElementKeyCache,

        // Full variable support
        BlueprintVariableSupport,

        // Hierarchy V2.0
        RigHierarchyV2,

        // RigHierarchy to support multi component parent constraints
        RigHierarchyMultiParentConstraints,

        // RigHierarchy now supports space favorites per control
        RigHierarchyControlSpaceFavorites,

        // RigHierarchy now stores min and max values as float storages
        StorageMinMaxValuesAsFloatStorage,

        // RenameGizmoToShape
        RenameGizmoToShape,

        // BoundVariableWithInjectionNode
        BoundVariableWithInjectionNode,

        // Switch limit control over to per channel limits
        PerChannelLimits,

        // Removed the parent cache for multi parent elements
        RemovedMultiParentParentCache,

        // Deprecation of parameters
        RemoveParameters,

        // Added rig curve element value state flag
        CurveElementValueStateFlag,

        // Added the notion of a per control animation type
        ControlAnimationType,

        // Added preferred permutation for templates
        TemplatesPreferredPermutatation,

        // Added preferred euler angles to controls
        PreferredEulerAnglesForControls,

        // Added rig hierarchy element metadata
        HierarchyElementMetadata,

        // Converted library nodes to templates
        LibraryNodeTemplates,

        // Controls to be able specify space switch targets
        RestrictSpaceSwitchingForControls,

        // Controls to be able specify which channels should be visible in sequencer
        ControlTransformChannelFiltering,

        // Store function information (and compilation data) in blueprint generated class
        StoreFunctionsInGeneratedClass,

        // Hierarchy storing previous names
        RigHierarchyStoringPreviousNames,

        // Control supporting preferred rotation order
        RigHierarchyControlPreferredRotationOrder,

        // Last bit required for Control supporting preferred rotation order
        RigHierarchyControlPreferredRotationOrderFlag,

        // Element metadata is now stored on URigHierarchy, rather than FRigBaseElement
        RigHierarchyStoresElementMetadata,

        // Add type (primary, secondary) and optional bool to FRigConnectorSettings
        ConnectorsWithType,

        // Add parent key to control rig pose
        RigPoseWithParentKey,

        // Physics solvers stored on hierarchy
        ControlRigStoresPhysicsSolvers,

        // -----<new versions can be added above this line>-------------------------------------------------
        VersionPlusOne,
        LatestVersion = VersionPlusOne - 1,
    }

    public static readonly FGuid GUID = new(0xA7820CFB, 0x20A74359, 0x8C542C14, 0x9623CF50);

    public static Type Get(FArchive Ar)
    {
        var ver = Ar.CustomVer(GUID);
        if (ver >= 0)
            return (Type) ver;

        return Ar.Game switch
        {
            < EGame.GAME_UE4_23 => Type.BeforeCustomVersionWasAdded,
            < EGame.GAME_UE4_25 => Type.OperatorsStoringPropertyPaths,
            < EGame.GAME_UE4_26 => Type.SwitchedToRigVM,
            < EGame.GAME_UE5_0 => Type.BlueprintVariableSupport,
            < EGame.GAME_UE5_1 => Type.PerChannelLimits,
            < EGame.GAME_UE5_2 => Type.LibraryNodeTemplates,
            < EGame.GAME_UE5_3 => Type.RigHierarchyStoringPreviousNames,
            < EGame.GAME_UE5_4 => Type.RigHierarchyControlPreferredRotationOrderFlag,
            < EGame.GAME_UE5_5 => Type.RigPoseWithParentKey,
            _ => Type.LatestVersion
        };
    }
}
