using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Objects.ControlRig;

public enum ESerializationPhase
{
    StaticData,
    InterElementData
};

public struct FRigComputedTransform(FAssetArchive Ar)
{
    public FTransform Transform = new FTransform(Ar);
    public bool bDirty = Ar.ReadBoolean();
}

public struct FRigLocalAndGlobalTransform(FAssetArchive Ar)
{
    public FRigComputedTransform Local = new FRigComputedTransform(Ar);
    public FRigComputedTransform Global = new FRigComputedTransform(Ar);
}

public struct FRigCurrentAndInitialTransform(FAssetArchive Ar)
{
    public FRigLocalAndGlobalTransform Current = new FRigLocalAndGlobalTransform(Ar);
    public FRigLocalAndGlobalTransform Initial = new FRigLocalAndGlobalTransform(Ar);
}

public class FRigBaseElement
{
    public URigHierarchy? Owner;
    public FRigElementKey LoadedKey;

    public virtual void Load(FAssetArchive Ar, URigHierarchy hierarchy, ESerializationPhase serializationPhase)
    {
        Owner = hierarchy;
        if (serializationPhase != ESerializationPhase.StaticData) return;

        LoadedKey = new FRigElementKey(Ar);

        if (FControlRigObjectVersion.Get(Ar) < FControlRigObjectVersion.Type.HierarchyElementMetadata ||
            FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.RigHierarchyStoresElementMetadata) return;

        //static const UEnum* MetadataTypeEnum = StaticEnum<ERigMetadataType>();
        var MetadataNum = Ar.Read<int>();
        for(var MetadataIndex = 0; MetadataIndex < MetadataNum; MetadataIndex++)
        {
            FName MetadataName = Ar.ReadFName();
            FName MetadataTypeName = Ar.ReadFName();
            FRigBaseMetadata Md = FRigBoolMetadata.Read(Ar, false);
        }
    }
}

public class FRigTransformElement : FRigBaseElement
{
    public FRigCurrentAndInitialTransform Pose;

    public override void Load(FAssetArchive Ar, URigHierarchy hierarchy, ESerializationPhase serializationPhase)
    {
        base.Load(Ar, hierarchy, serializationPhase);
        if (serializationPhase == ESerializationPhase.StaticData)
            Pose = new FRigCurrentAndInitialTransform(Ar);
    }
}

public class FRigSingleParentElement : FRigTransformElement
{
    public FRigElementKey ParentKey;

    public override void Load(FAssetArchive Ar, URigHierarchy hierarchy, ESerializationPhase serializationPhase)
    {
        base.Load(Ar, hierarchy, serializationPhase);

        if (serializationPhase != ESerializationPhase.InterElementData) return;

        ParentKey = new FRigElementKey(Ar);
    }
}

public struct FRigElementWeight(float value)
{
    float Location = value;
    float Rotation = value;
    float Scale = value;
}

public struct FRigElementParentConstraint
{
    public FRigTransformElement ParentElement;
    public FRigElementWeight Weight;
    public FRigElementWeight InitialWeight;
    // mutable FRigComputedTransform Cache;
    public bool bCacheIsDirty;
}

public class FRigMultiParentElement : FRigTransformElement
{
    public FRigCurrentAndInitialTransform Parent;
    public FRigElementParentConstraint[] ParentConstraints;
    public Dictionary<FRigElementKey, int> IndexLookup = [];

    public override void Load(FAssetArchive Ar, URigHierarchy hierarchy, ESerializationPhase serializationPhase)
    {
        base.Load(Ar, hierarchy, serializationPhase);
        if(serializationPhase == ESerializationPhase.StaticData)
        {
            if (FControlRigObjectVersion.Get(Ar) < FControlRigObjectVersion.Type.RemovedMultiParentParentCache)
            {
                Parent = new FRigCurrentAndInitialTransform(Ar);
            }

            var NumParents = Ar.Read<int>();
            ParentConstraints = new FRigElementParentConstraint[NumParents];
        }
        else if(serializationPhase == ESerializationPhase.InterElementData)
        {
            for(var ParentIndex = 0; ParentIndex < ParentConstraints.Length; ParentIndex++)
            {
                FRigElementParentConstraint constraint = new();
                FRigElementKey ParentKey = new FRigElementKey(Ar);
                constraint.bCacheIsDirty = true;

                if (FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.RigHierarchyMultiParentConstraints)
                {
                    constraint.InitialWeight = Ar.Read<FRigElementWeight>();
                    constraint.Weight = Ar.Read<FRigElementWeight>();
                }
                else
                {
                    constraint.InitialWeight = new FRigElementWeight(Ar.Read<float>());
                    constraint.Weight = new FRigElementWeight(Ar.Read<float>());
                }

                ParentConstraints[ParentIndex] = constraint;
                IndexLookup.Add(ParentKey, ParentIndex);
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
    ERigBoneType BoneType;

    public override void Load(FAssetArchive Ar, URigHierarchy hierarchy, ESerializationPhase serializationPhase)
    {
        base.Load(Ar, hierarchy, serializationPhase);

        if (serializationPhase != ESerializationPhase.StaticData) return;
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

public struct FRigControlLimitEnabled(FAssetArchive Ar)
{
    bool bMinimum = Ar.ReadBoolean();
    bool bMaximum = Ar.ReadBoolean();
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

    public FRigControlElementCustomization(FAssetArchive Ar)
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
    ERigControlAnimationType AnimationType;
    ERigControlType ControlType;
    FName DisplayName;
    /** the primary axis to use for float controls */
    ERigControlAxis PrimaryAxis;
    /** If Created from a Curve  Container*/
    bool bIsCurve;
    /** True if the control has limits. */
    FRigControlLimitEnabled[] LimitEnabled;
    bool bDrawLimits;
    /** The minimum limit of the control's value */
    FRigControlValue MinimumValue;
    /** The maximum limit of the control's value */
    FRigControlValue MaximumValue;
    /** Set to true if the shape is currently visible in 3d */
    bool bShapeVisible;
    /** Defines how the shape visibility should be changed */
    ERigControlVisibility ShapeVisibility;
    /* This is optional UI setting - this doesn't mean this is always used, but it is optional for manipulation layer to use this*/
    FName ShapeName;
    FLinearColor ShapeColor;
    /** If the control is transient and only visible in the control rig editor */
    bool bIsTransientControl;
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

    public FRigControlSettings(FAssetArchive Ar)
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

public struct FRigPreferredEulerAngles(FAssetArchive Ar)
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

    public override void Load(FAssetArchive Ar, URigHierarchy hierarchy, ESerializationPhase serializationPhase)
    {
        base.Load(Ar, hierarchy, serializationPhase);

        if (serializationPhase != ESerializationPhase.StaticData) return;

        Settings = new FRigControlSettings(Ar);
        Offset = new FRigCurrentAndInitialTransform(Ar);
        Shape = new FRigCurrentAndInitialTransform(Ar);
        if (FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.PreferredEulerAnglesForControls)
        {
            PreferredEulerAngles = new FRigPreferredEulerAngles(Ar);
        }
    }
}

public class FRigCurveElement : FRigBaseElement
{
    public float Value;

    public override void Load(FAssetArchive Ar, URigHierarchy hierarchy, ESerializationPhase SerializationPhase)
    {
        base.Load(Ar, hierarchy, SerializationPhase);

        if (SerializationPhase == ESerializationPhase.InterElementData) return;
        var bIsValueSet = Ar.Game >= EGame.GAME_UE5_1 ? Ar.ReadBoolean() : false;
        Value = Ar.Read<float>();
    }
}

public struct FRigRigidBodySettings(FAssetArchive Ar)
{
    float Mass = Ar.Read<float>();
}

public class FRigRigidBodyElement : FRigSingleParentElement
{
    public FRigRigidBodySettings Settings;

    public override void Load(FAssetArchive Ar, URigHierarchy hierarchy, ESerializationPhase serializationPhase)
    {
        base.Load(Ar, hierarchy, serializationPhase);

        if (serializationPhase != ESerializationPhase.StaticData) return;

        Settings = new FRigRigidBodySettings(Ar);
    }
}

public class FRigReferenceElement : FRigSingleParentElement;

public enum EConnectorType : byte
{
    Primary, // Single primary connector, non-optional and always visible. When dropped on another element, this connector will resolve to that element.
    Secondary, // Could be multiple, can auto-solve (visible if not solved), can be optional
}

public struct FRigConnectionRuleStash(FAssetArchive Ar)
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

    public FRigConnectorSettings(FAssetArchive Ar)
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

    public override void Load(FAssetArchive Ar, URigHierarchy hierarchy, ESerializationPhase serializationPhase)
    {
        base.Load(Ar, hierarchy, serializationPhase);

        if (serializationPhase != ESerializationPhase.StaticData) return;

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
    Reference = 32,
    Connector = 64,
    Socket = 128
}
