using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.ControlRig;
using CUE4Parse.UE4.Assets.Exports.ControlRig.Rigs.Elements;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.ControlRig;

public enum ESerializationPhase
{
    StaticData,
    InterElementData
};

public struct FRigComputedTransform
{
    public FTransform Transform;
    public bool bDirty;

    public FRigComputedTransform(FArchive Ar, FRigHierarchySerializationSettings inSettings)
    {
        if (inSettings.ControlRigVersion < FControlRigObjectVersion.Type.RigHierarchyCompactTransformSerialization)
        {
            Transform = new FTransform(Ar);
            bDirty = Ar.ReadBoolean();
        }
        else
        {
            bDirty = Ar.ReadBoolean();
            if (!bDirty)
            {
                var rigTransform = new FRigCompactTransform(Transform);
                rigTransform.Load(Ar, inSettings);

                Transform = rigTransform.Transform;
            }
        }
    }
}

public struct FRigLocalAndGlobalTransform
{
    public FRigComputedTransform Local;
    public FRigComputedTransform Global;

    public FRigLocalAndGlobalTransform(FArchive Ar, FRigHierarchySerializationSettings inSettings)
    {
        if (inSettings.bSerializeLocalTransform)
        {
            Local = new FRigComputedTransform(Ar, inSettings);
        }

        if (inSettings.bSerializeGlobalTransform)
        {
            Global = new FRigComputedTransform(Ar, inSettings);
        }
    }
}

public struct FRigCurrentAndInitialTransform
{
    public FRigLocalAndGlobalTransform Current;
    public FRigLocalAndGlobalTransform Initial;

    public FRigCurrentAndInitialTransform(FArchive Ar, FRigHierarchySerializationSettings inSettings)
    {
        if (inSettings.bSerializeCurrentTransform)
        {
            Current = new FRigLocalAndGlobalTransform(Ar, inSettings);
        }

        if (inSettings.bSerializeInitialTransform)
        {
            Initial = new FRigLocalAndGlobalTransform(Ar, inSettings);
        }
    }
}

public class FRigBaseElement
{
    [JsonIgnore] public URigHierarchy? Owner;
    public FRigElementKey LoadedKey;

    public virtual void Load(FArchive Ar, URigHierarchy hierarchy, FRigHierarchySerializationSettings inSettings)
    {
        Owner = hierarchy;
        if (inSettings.SerializationPhase != ESerializationPhase.StaticData) return;

        LoadedKey = new FRigElementKey(Ar);

        if (FControlRigObjectVersion.Get(Ar) < FControlRigObjectVersion.Type.HierarchyElementMetadata ||
            FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.RigHierarchyStoresElementMetadata) return;

        //static const UEnum* MetadataTypeEnum = StaticEnum<ERigMetadataType>();
        var metadataNum = Ar.Read<int>();
        for(var metadataIndex = 0; metadataIndex < metadataNum; metadataIndex++)
        {
            _ = Ar.ReadFName();
            _ = Ar.ReadFName();
            _ = FRigBoolMetadata.Read(Ar, false);
        }
    }
}

public class FRigTransformElement : FRigBaseElement
{
    public FRigCurrentAndInitialTransform PoseStorage;

    public override void Load(FArchive Ar, URigHierarchy hierarchy, FRigHierarchySerializationSettings inSettings)
    {
        base.Load(Ar, hierarchy, inSettings);
        if (inSettings.SerializationPhase == ESerializationPhase.StaticData)
            PoseStorage = new FRigCurrentAndInitialTransform(Ar, inSettings);
    }
}

public class FRigSingleParentElement : FRigTransformElement
{
    public FRigElementKey ParentKey;

    public override void Load(FArchive Ar, URigHierarchy hierarchy, FRigHierarchySerializationSettings inSettings)
    {
        base.Load(Ar, hierarchy, inSettings);

        if (inSettings.SerializationPhase != ESerializationPhase.InterElementData) return;

        ParentKey = new FRigElementKey(Ar);
    }
}

public struct FRigElementWeight(float value)
{
    public float Location = value;
    public float Rotation = value;
    public float Scale = value;
}

public struct FRigElementParentConstraint
{
    public FRigTransformElement ParentElement;
    public FRigElementWeight Weight;
    public FRigElementWeight InitialWeight;
    public FName DisplayLabel;
    // mutable FRigComputedTransform Cache;
    public bool bCacheIsDirty;
}

public class FRigMultiParentElement : FRigTransformElement
{
    public FRigCurrentAndInitialTransform Parent;
    public FRigElementParentConstraint[] ParentConstraints;
    public Dictionary<FRigElementKey, int> IndexLookup = [];

    public override void Load(FArchive Ar, URigHierarchy hierarchy, FRigHierarchySerializationSettings inSettings)
    {
        base.Load(Ar, hierarchy, inSettings);

        if (inSettings.SerializationPhase == ESerializationPhase.StaticData)
        {
            if (FControlRigObjectVersion.Get(Ar) < FControlRigObjectVersion.Type.RemovedMultiParentParentCache)
            {
                Parent = new FRigCurrentAndInitialTransform(Ar, inSettings);
            }

            var numParents = Ar.Read<int>();
            ParentConstraints = new FRigElementParentConstraint[numParents];
        }
        else if (inSettings.SerializationPhase == ESerializationPhase.InterElementData)
        {
            for (var parentIndex = 0; parentIndex < ParentConstraints.Length; parentIndex++)
            {
                var parentKey = new FRigElementKey(Ar);
                FRigElementParentConstraint constraint = new() { bCacheIsDirty = true };

                if (FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.RigHierarchyMultiParentConstraints)
                {
                    constraint.InitialWeight = Ar.Read<FRigElementWeight>();
                    constraint.Weight = Ar.Read<FRigElementWeight>();
                }
                else
                {
                    var initialWeight = Ar.Read<float>();
                    constraint.InitialWeight = new FRigElementWeight(initialWeight);

                    var weight = Ar.Read<float>();
                    constraint.Weight = new FRigElementWeight(weight);
                }

                if (FControlRigObjectVersion.Get(Ar) < FControlRigObjectVersion.Type.RigHierarchyParentContraintWithLabel)
                {
                    constraint.DisplayLabel = new FName("None");
                }
                else
                {
                    constraint.DisplayLabel = Ar.ReadFName();
                }

                ParentConstraints[parentIndex] = constraint;
                IndexLookup.Add(parentKey, parentIndex);
            }
        }
    }
}

public enum ERigBoneType
{
    Imported,
    User,
}

public class FRigBoneElement : FRigSingleParentElement
{
    public FName TypeName;
    public ERigBoneType BoneType;

    public override void Load(FArchive Ar, URigHierarchy hierarchy, FRigHierarchySerializationSettings inSettings)
    {
        base.Load(Ar, hierarchy, inSettings);

        if (inSettings.SerializationPhase != ESerializationPhase.StaticData) return;
        BoneType = EnumUtils.GetValueByName<ERigBoneType>(Ar.ReadFName().Text);
    }
}

public enum ERigControlAnimationType : byte
{
    // A visible, animatable control.
    AnimationControl,
    // An animation channel without a 3d shape
    AnimationChannel,
    // A control to drive other controls,
    // not animatable in sequencer.
    ProxyControl,
    // Visual feedback only - the control is
    // neither animatable nor selectable.
    VisualCue
};

public enum ERigControlType : byte
{
    Bool,
    Float,
    Integer,
    Vector2D,
    Position,
    Scale,
    Rotator,
    Transform,
    TransformNoScale,
    EulerTransform,
    ScaleFloat,
};

public enum ERigControlAxis : byte
{
    X,
    Y,
    Z
};

public enum ERigControlVisibility : byte
{
    // Visibility controlled by the graph
    UserDefined,
    // Visibility Controlled by the selection of driven controls
    BasedOnSelection
};

public enum ERigControlTransformChannel : byte
{
    TranslationX,
    TranslationY,
    TranslationZ,
    Pitch,
    Yaw,
    Roll,
    ScaleX,
    ScaleY,
    ScaleZ
};

public enum EEulerRotationOrder : byte
{
    XYZ,
    XZY,
    YXZ,
    YZX,
    ZXY,
    ZYX
};

public struct FRigControlLimitEnabled(FArchive Ar)
{
    public bool bMinimum = Ar.ReadBoolean();
    public bool bMaximum = Ar.ReadBoolean();
}

public struct FRigControlValue
{
    public float Float00;
    public float Float01;
    public float Float02;
    public float Float03;
    public float Float10;
    public float Float11;
    public float Float12;
    public float Float13;
    public float Float20;
    public float Float21;
    public float Float22;
    public float Float23;
    public float Float30;
    public float Float31;
    public float Float32;
    public float Float33;
    public float Float00_2;
    public float Float01_2;
    public float Float02_2;
    public float Float03_2;
    public float Float10_2;
    public float Float11_2;
    public float Float12_2;
    public float Float13_2;
    public float Float20_2;
    public float Float21_2;
    public float Float22_2;
    public float Float23_2;
    public float Float30_2;
    public float Float31_2;
    public float Float32_2;
    public float Float33_2;
}

public struct FRigControlElementCustomization
{
    public FRigElementKey[] AvailableSpaces;
    public FRigElementKey[] RemovedSpaces = [];

    public FRigControlElementCustomization(FArchive Ar)
    {
        AvailableSpaces = Ar.ReadArray(() => new FRigElementKey(Ar));
    }

    public FRigControlElementCustomization(FRigElementKey[] availableSpaces, FRigElementKey[] removedSpaces)
    {
        AvailableSpaces = availableSpaces;
        RemovedSpaces = removedSpaces;
    }
}

public struct FRigControlSettings
{
    public ERigControlAnimationType AnimationType;
    public ERigControlType ControlType;
    public FName DisplayName;
    /** the primary axis to use for float controls */
    public ERigControlAxis PrimaryAxis;
    /** If Created from a Curve  Container*/
    public bool bIsCurve;
    /** True if the control has limits. */
    public FRigControlLimitEnabled[] LimitEnabled;
    public bool bDrawLimits;
    /** The minimum limit of the control's value */
    public FRigControlValue MinimumValue;
    /** The maximum limit of the control's value */
    public FRigControlValue MaximumValue;
    /** Set to true if the shape is currently visible in 3d */
    public bool bShapeVisible;
    /** Defines how the shape visibility should be changed */
    public ERigControlVisibility ShapeVisibility;
    /* This is optional UI setting - this doesn't mean this is always used, but it is optional for manipulation layer to use this*/
    public FName ShapeName;
    public FLinearColor ShapeColor;
    /** If the control is transient and only visible in the control rig editor */
    public bool bIsTransientControl;
    /** If the control is integer it can use this enum to choose values */
    //TObjectPtr<UEnum> ControlEnum;

    // The User interface customization used for a control
    // This will be used as the default content for the space picker and other widgets
    public FRigControlElementCustomization Customization;
    // The list of driven controls for this proxy control.
    public FRigElementKey[] DrivenControls;
    // The list of previously driven controls - prior to a procedural change
    public FRigElementKey[] PreviouslyDrivenControls;
    // If set to true the animation channel will be grouped with the parent control in sequencer
    public bool bGroupWithParentControl;
    // Allow to space switch only to the available spaces
    public bool bRestrictSpaceSwitching;
    // Filtered Visible Transform channels. If this is empty everything is visible
    public ERigControlTransformChannel[] FilteredChannels;
    // The euler rotation order this control prefers for animation, if we aren't using default UE rotator
    public EEulerRotationOrder PreferredRotationOrder;
    // Whether to use a specified rotation order or just use the default FRotator order and conversion functions
    public bool bUsePreferredRotationOrder;

    public FRigControlSettings(FArchive Ar)
    {
        FName AnimationTypeName, ControlTypeName, ShapeVisibilityName, PrimaryAxisName;
        string ControlEnumPathName;

        bool bLimitTranslation_DEPRECATED = false;
        bool bLimitRotation_DEPRECATED = false;
        bool bLimitScale_DEPRECATED = false;
        bool bAnimatableDeprecated = false;
        bool bShapeEnabledDeprecated = false;

        if (FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.ControlAnimationType)
        {
            AnimationTypeName = Ar.ReadFName();
        }

        ControlTypeName = Ar.ReadFName();
        DisplayName = Ar.ReadFName();
        PrimaryAxisName = Ar.ReadFName();
        bIsCurve = Ar.ReadBoolean();


        if (FControlRigObjectVersion.Get(Ar) < FControlRigObjectVersion.Type.ControlAnimationType)
        {
            bAnimatableDeprecated = Ar.ReadBoolean();
        }

        if (FControlRigObjectVersion.Get(Ar) < FControlRigObjectVersion.Type.PerChannelLimits)
        {
            bLimitTranslation_DEPRECATED = Ar.ReadBoolean();
            bLimitRotation_DEPRECATED = Ar.ReadBoolean();
            bLimitScale_DEPRECATED = Ar.ReadBoolean();
        }
        else
        {
            LimitEnabled = Ar.ReadArray(() => new FRigControlLimitEnabled(Ar));
        }

        bDrawLimits = Ar.ReadBoolean();

        FTransform MinimumTransform, MaximumTransform;
        if (FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.StorageMinMaxValuesAsFloatStorage)
        {
            MinimumValue = Ar.Read<FRigControlValue>();
            MaximumValue = Ar.Read<FRigControlValue>();
        }
        else
        {
            MinimumTransform = new FTransform(Ar);
            MaximumTransform = new FTransform(Ar);
        }

        ControlType = EnumUtils.GetValueByName<ERigControlType>(ControlTypeName.Text);

        if (FControlRigObjectVersion.Get(Ar) < FControlRigObjectVersion.Type.ControlAnimationType)
        {
            bShapeEnabledDeprecated = Ar.ReadBoolean();
            //SetAnimationTypeFromDeprecatedData(bAnimatableDeprecated, bShapeEnabledDeprecated);
            //AnimationTypeName = AnimationTypeEnum->GetNameByValue((int64)AnimationType);
        }

        bShapeVisible = Ar.ReadBoolean();

        if (FControlRigObjectVersion.Get(Ar) < FControlRigObjectVersion.Type.ControlAnimationType)
        {
            ShapeVisibilityName = ERigControlVisibility.UserDefined.ToString();
            //ShapeVisibilityName = ShapeVisibilityEnum->GetNameByValue((int64) ERigControlVisibility::UserDefined);
        }
        else
        {
            ShapeVisibilityName = Ar.ReadFName();
        }

        ShapeName = Ar.ReadFName();

        if (FControlRigObjectVersion.Get(Ar) < FControlRigObjectVersion.Type.RenameGizmoToShape)
        {
            //	if(ShapeName == FRigControl().GizmoName)
            //	{
            //		ShapeName = FControlRigShapeDefinition().ShapeName;
            //	}
        }

        ShapeColor = Ar.Read<FLinearColor>();
        bIsTransientControl = Ar.ReadBoolean();
        ControlEnumPathName = Ar.ReadFString();

        //AnimationType = (ERigControlAnimationType)AnimationTypeEnum->GetValueByName(AnimationTypeName);
        //PrimaryAxis = (ERigControlAxis)ControlAxisEnum->GetValueByName(PrimaryAxisName);
        //ShapeVisibility = (ERigControlVisibility)ShapeVisibilityEnum->GetValueByName(ShapeVisibilityName);

        if (FControlRigObjectVersion.Get(Ar) < FControlRigObjectVersion.Type.StorageMinMaxValuesAsFloatStorage)
        {
            //	MinimumValue.SetFromTransform(MinimumTransform, ControlType, PrimaryAxis);
            //	MaximumValue.SetFromTransform(MaximumTransform, ControlType, PrimaryAxis);
        }

        //ControlEnum = nullptr;
        //if(!ControlEnumPathName.IsEmpty())
        //{
        //	if (IsInGameThread())
        //	{
        //		ControlEnum = LoadObject<UEnum>(nullptr, *ControlEnumPathName);
        //	}
        //	else
        //	{
        //		ControlEnum = FindObject<UEnum>(nullptr, *ControlEnumPathName);
        //	}
        //}

        if (FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.RigHierarchyControlSpaceFavorites)
        {
            Customization = new FRigControlElementCustomization(Ar);
        }
        else
        {
            Customization = new FRigControlElementCustomization([], []);
        }

        if (FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.ControlAnimationType)
        {
            DrivenControls = Ar.ReadArray(() => new FRigElementKey(Ar));
        }
        else
        {
            DrivenControls = [];
        }

        PreviouslyDrivenControls = [];

        if (FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.PerChannelLimits)
        {
            //SetupLimitArrayForType(bLimitTranslation_DEPRECATED, bLimitRotation_DEPRECATED, bLimitScale_DEPRECATED);
        }

        if (FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.ControlAnimationType)
        {
            bGroupWithParentControl = Ar.ReadBoolean();
        }
        else
        {
            //bGroupWithParentControl = IsAnimatable() && (
            //	ControlType == ERigControlType::Bool ||
            //	ControlType == ERigControlType::Float ||
            //	ControlType == ERigControlType::ScaleFloat ||
            //	ControlType == ERigControlType::Integer ||
            //	ControlType == ERigControlType::Vector2D
            //);
        }

        bRestrictSpaceSwitching = false;
        if (FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.RestrictSpaceSwitchingForControls)
        {
            bRestrictSpaceSwitching = Ar.ReadBoolean();
        }

        FilteredChannels = [];
        if (FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.ControlTransformChannelFiltering)
        {
            FilteredChannels = Ar.ReadArray(Ar.Read<ERigControlTransformChannel>);
        }

        PreferredRotationOrder = EEulerRotationOrder.YZX;
        if (FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.RigHierarchyControlPreferredRotationOrder)
        {
            PreferredRotationOrder = Ar.Read<EEulerRotationOrder>();
        }

        bUsePreferredRotationOrder = false;
        if (FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.RigHierarchyControlPreferredRotationOrderFlag)
        {
            bUsePreferredRotationOrder = Ar.ReadBoolean();
        }
    }
}

public struct FRigPreferredEulerAngles(FArchive Ar)
{
    EEulerRotationOrder RotationOrder = EnumUtils.GetValueByName<EEulerRotationOrder>(Ar.ReadFName().Text);
    FVector Current = new FVector(Ar);
    FVector Initial = new FVector(Ar);
}

public class FRigNullElement : FRigMultiParentElement;
public class FRigControlElement : FRigMultiParentElement
{
    FRigControlSettings Settings;
    FRigCurrentAndInitialTransform Offset;
    FRigCurrentAndInitialTransform Shape;
    FRigPreferredEulerAngles? PreferredEulerAngles;

    public override void Load(FArchive Ar, URigHierarchy hierarchy, FRigHierarchySerializationSettings inSettings)
    {
        base.Load(Ar, hierarchy, inSettings);

        if (inSettings.SerializationPhase != ESerializationPhase.StaticData) return;

        Settings = new FRigControlSettings(Ar);
        Offset = new FRigCurrentAndInitialTransform(Ar, inSettings);
        Shape = new FRigCurrentAndInitialTransform(Ar, inSettings);
        if (FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.PreferredEulerAnglesForControls)
        {
            PreferredEulerAngles = new FRigPreferredEulerAngles(Ar);
        }
    }
}

public class FRigCurveElement : FRigBaseElement
{
    public bool bIsValueSet;
    public float Value;

    public override void Load(FArchive Ar, URigHierarchy hierarchy, FRigHierarchySerializationSettings inSettings)
    {
        base.Load(Ar, hierarchy, inSettings);

        if (inSettings.SerializationPhase == ESerializationPhase.InterElementData) return;

        if (FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.CurveElementValueStateFlag)
        {
            bIsValueSet = Ar.ReadBoolean();
        }
        else
        {
            bIsValueSet = true;
        }

        Value = Ar.Read<float>();
    }
}

public struct FRigRigidBodySettings(FArchive Ar)
{
    float Mass = Ar.Read<float>();
}

public class FRigRigidBodyElement : FRigSingleParentElement
{
    public FRigRigidBodySettings Settings;

    public override void Load(FArchive Ar, URigHierarchy hierarchy, FRigHierarchySerializationSettings inSettings)
    {
        base.Load(Ar, hierarchy, inSettings);

        if (inSettings.SerializationPhase != ESerializationPhase.StaticData) return;

        Settings = new FRigRigidBodySettings(Ar);
    }
}

public class FRigReferenceElement : FRigSingleParentElement;

public enum EConnectorType : byte
{
    Primary, // Single primary connector, non-optional and always visible. When dropped on another element, this connector will resolve to that element.
    Secondary, // Could be multiple, can auto-solve (visible if not solved), can be optional
}

public struct FRigConnectionRuleStash(FArchive Ar)
{
    public string ScriptStructPath = Ar.ReadFString();
    public string ExportedText = Ar.ReadFString();
}

public struct FRigConnectorSettings
{
    public string Description;
    public EConnectorType Type = EConnectorType.Primary;
    public bool bOptional = false;
    public FRigConnectionRuleStash[] Rules;

    public FRigConnectorSettings(FArchive Ar)
    {
        Description = Ar.ReadFString();
        if (FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.ConnectorsWithType)
        {
            Type = Ar.Read<EConnectorType>();
            bOptional = Ar.ReadBoolean();
        }

        Rules = Ar.ReadArray(() => new FRigConnectionRuleStash(Ar));
    }
}

public class FRigConnectorElement : FRigBaseElement
{
    public FRigConnectorSettings Settings;

    public override void Load(FArchive Ar, URigHierarchy hierarchy, FRigHierarchySerializationSettings inSettings)
    {
        base.Load(Ar, hierarchy, inSettings);

        if (inSettings.SerializationPhase != ESerializationPhase.StaticData) return;

        Settings = new FRigConnectorSettings(Ar);
    }
}

public class FRigSocketElement : FRigSingleParentElement;

public enum ERigElementType : byte
{
    None = 0,
    Bone = 1,
    Null = 2,
    Space = Null,
    Control = 4,
    Curve = 8,
    RigidBody = 16,
    Physics = 16,
    Reference = 32,
    Connector = 64,
    Socket = 128,

    All = Bone | Null | Control | Curve | Physics | Reference | Connector | Socket,
}
