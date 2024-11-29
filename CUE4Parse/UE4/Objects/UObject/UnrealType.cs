using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.UObject;

[Flags]
public enum EPropertyFlags : ulong
{
    None = 0,

    /// <summary>
    /// Property is user-settable in the editor.
    /// </summary>
    Edit = 0x0000000000000001,

    /// <summary>
    /// This is a constant function parameter
    /// </summary>
    ConstParm = 0x0000000000000002,

    /// <summary>
    /// This property can be read by blueprint code
    /// </summary>
    BlueprintVisible = 0x0000000000000004,

    /// <summary>
    /// Object can be exported with actor.
    /// </summary>
    ExportObject = 0x0000000000000008,

    /// <summary>
    /// This property cannot be modified by blueprint code
    /// </summary>
    BlueprintReadOnly = 0x0000000000000010,

    /// <summary>
    /// Property is relevant to network replication.
    /// </summary>
    Net = 0x0000000000000020,

    /// <summary>
    /// Indicates that elements of an array can be modified, but its size cannot be changed.
    /// </summary>
    EditFixedSize = 0x0000000000000040,

    /// <summary>
    /// Function/When call parameter.
    /// </summary>
    Parm = 0x0000000000000080,

    /// <summary>
    /// Value is copied out after function call.
    /// </summary>
    OutParm = 0x0000000000000100,

    /// <summary>
    /// memset is fine for construction
    /// </summary>
    ZeroConstructor = 0x0000000000000200,

    /// <summary>
    /// Return value.
    /// </summary>
    ReturnParm = 0x0000000000000400,

    /// <summary>
    /// Disable editing of this property on an archetype/sub-blueprint
    /// </summary>
    DisableEditOnTemplate = 0x0000000000000800,

    /// <summary>
    /// Object property can never be null
    /// </summary>
    NonNullable = 0x0000000000001000,

    /// <summary>
    /// Property is transient: shouldn't be saved or loaded, except for Blueprint CDOs.
    /// </summary>
    Transient = 0x0000000000002000,

    /// <summary>
    /// Property should be loaded/saved as permanent profile.
    /// </summary>
    Config = 0x0000000000004000,

    /// <summary>
    /// Parameter is required in blueprint. Not linking the parameter with a node will result in a compile error.
    /// </summary>
    RequiredParm = 0x0000000000008000,

    /// <summary>
    /// Disable editing on an instance of this class
    /// </summary>
    DisableEditOnInstance = 0x0000000000010000,

    /// <summary>
    /// Property is uneditable in the editor.
    /// </summary>
    EditConst = 0x0000000000020000,

    /// <summary>
    /// Load config from base class, not subclass.
    /// </summary>
    GlobalConfig = 0x0000000000040000,

    /// <summary>
    /// Property is a component references.
    /// </summary>
    InstancedReference = 0x0000000000080000,

    // Unused = 0x0000000000100000,

    /// <summary>
    /// Property should always be reset to the default value during any type of duplication (copy/paste, binary duplication, etc.)
    /// </summary>
    DuplicateTransient = 0x0000000000200000,

    // Unused = 0x0000000000400000,
    // Unused = 0x0000000000800000,

    /// <summary>
    /// Property should be serialized for save games, this is only checked for game-specific archives with ArIsSaveGame
    /// </summary>
    SaveGame = 0x0000000001000000,

    /// <summary>
    /// Hide clear (and browse) button.
    /// </summary>
    NoClear = 0x0000000002000000,

    // Unused = 0x0000000004000000,

    /// <summary>
    /// Value is passed by reference; "OutParam" and "Param" should also be set.
    /// </summary>
    ReferenceParm = 0x0000000008000000,

    /// <summary>
    /// MC Delegates only.  Property should be exposed for assigning in blueprint code
    /// </summary>
    BlueprintAssignable = 0x0000000010000000,

    /// <summary>
    /// Property is deprecated.  Read it from an archive, but don't save it.
    /// </summary>
    Deprecated = 0x0000000020000000,

    /// <summary>
    /// If this is set, then the property can be memcopied instead of CopyCompleteValue / CopySingleValue
    /// </summary>
    IsPlainOldData = 0x0000000040000000,

    /// <summary>
    /// Not replicated. For non replicated properties in replicated structs
    /// </summary>
    RepSkip = 0x0000000080000000,

    /// <summary>
    /// Notify actors when a property is replicated
    /// </summary>
    RepNotify = 0x0000000100000000,

    /// <summary>
    /// interpolatable property for use with cinematics
    /// </summary>
    Interp = 0x0000000200000000,

    /// <summary>
    /// Property isn't transacted
    /// </summary>
    NonTransactional = 0x0000000400000000,

    /// <summary>
    /// Property should only be loaded in the editor
    /// </summary>
    EditorOnly = 0x0000000800000000,

    /// <summary>
    /// No destructor
    /// </summary>
    NoDestructor = 0x0000001000000000,

    // Unused = 0x0000002000000000,

    /// <summary>
    /// Only used for weak pointers, means the export type is autoweak
    /// </summary>
    AutoWeak = 0x0000004000000000,

    /// <summary>
    /// Property contains component references.
    /// </summary>
    ContainsInstancedReference = 0x0000008000000000,

    /// <summary>
    /// asset instances will add properties with this flag to the asset registry automatically
    /// </summary>
    AssetRegistrySearchable = 0x0000010000000000,

    /// <summary>
    /// The property is visible by default in the editor details view
    /// </summary>
    SimpleDisplay = 0x0000020000000000,

    /// <summary>
    /// The property is advanced and not visible by default in the editor details view
    /// </summary>
    AdvancedDisplay = 0x0000040000000000,

    /// <summary>
    /// property is protected from the perspective of script
    /// </summary>
    Protected = 0x0000080000000000,

    /// <summary>
    /// MC Delegates only.  Property should be exposed for calling in blueprint code
    /// </summary>
    BlueprintCallable = 0x0000100000000000,

    /// <summary>
    /// MC Delegates only.  This delegate accepts (only in blueprint) only events with BlueprintAuthorityOnly.
    /// </summary>
    BlueprintAuthorityOnly = 0x0000200000000000,

    /// <summary>
    /// Property shouldn't be exported to text format (e.g. copy/paste)
    /// </summary>
    TextExportTransient = 0x0000400000000000,

    /// <summary>
    /// Property should only be copied in PIE
    /// </summary>
    NonPIEDuplicateTransient = 0x0000800000000000,

    /// <summary>
    /// Property is exposed on spawn
    /// </summary>
    ExposeOnSpawn = 0x0001000000000000,

    /// <summary>
    /// A object referenced by the property is duplicated like a component. (Each actor should have an own instance.)
    /// </summary>
    PersistentInstance = 0x0002000000000000,

    /// <summary>
    /// Property was parsed as a wrapper class like TSubclassOf&lt;T&gt;, FScriptInterface etc., rather than a USomething*
    /// </summary>
    UObjectWrapper = 0x0004000000000000,

    /// <summary>
    /// This property can generate a meaningful hash value.
    /// </summary>
    HasGetValueTypeHash = 0x0008000000000000,

    /// <summary>
    /// Public native access specifier
    /// </summary>
    NativeAccessSpecifierPublic = 0x0010000000000000,

    /// <summary>
    /// Protected native access specifier
    /// </summary>
    NativeAccessSpecifierProtected = 0x0020000000000000,

    /// <summary>
    /// Private native access specifier
    /// </summary>
    NativeAccessSpecifierPrivate = 0x0040000000000000,

    /// <summary>
    /// Property shouldn't be serialized, can still be exported to text
    /// </summary>
    SkipSerialization = 0x0080000000000000,

    /// <summary>
    /// All Native Access Specifier flags
    /// </summary>
    NativeAccessSpecifiers = NativeAccessSpecifierPublic | NativeAccessSpecifierProtected | NativeAccessSpecifierPrivate,

    /// <summary>
    /// All parameter flags
    /// </summary>
    ParmFlags = Parm | OutParm | ReturnParm | ReferenceParm | ConstParm | RequiredParm,

    /// <summary>
    /// Flags that are propagated to properties inside array container
    /// </summary>
    PropagateToArrayInner =	ExportObject | PersistentInstance | InstancedReference | ContainsInstancedReference | Config | EditConst | Deprecated | EditorOnly | AutoWeak | UObjectWrapper,

    /// <summary>
    /// Flags that are propagated to value properties inside map container
    /// </summary>
    PropagateToMapValue = ExportObject | PersistentInstance | InstancedReference | ContainsInstancedReference | Config | EditConst | Deprecated | EditorOnly | AutoWeak | UObjectWrapper | Edit,

    /// <summary>
    /// Flags that are propagated to key properties inside map container
    /// </summary>
    PropagateToMapKey = ExportObject | PersistentInstance | InstancedReference | ContainsInstancedReference | Config | EditConst | Deprecated | EditorOnly | AutoWeak | UObjectWrapper | Edit,

    /// <summary>
    /// Flags that are propagated to properties inside set container
    /// </summary>
    PropagateToSetElement = ExportObject | PersistentInstance | InstancedReference | ContainsInstancedReference | Config | EditConst | Deprecated | EditorOnly | AutoWeak | UObjectWrapper | Edit,

    /// <summary>
    /// The flags that should never be set on interface properties
    /// </summary>
    InterfaceClearMask = ExportObject|InstancedReference|ContainsInstancedReference,

    /// <summary>
    /// All the properties that can be stripped for final release console builds
    /// </summary>
    DevelopmentAssets = EditorOnly,

    /// <summary>
    /// All the properties that should never be loaded or saved
    /// </summary>
    ComputedFlags = IsPlainOldData | NoDestructor | ZeroConstructor | HasGetValueTypeHash,

    /// <summary>
    /// Mask of all property flags
    /// </summary>
    AllFlags = ulong.MaxValue,
}

public class FProperty : FField
{
    public int ArrayDim;
    public int ElementSize;
    public EPropertyFlags PropertyFlags;
    public ushort RepIndex;
    public FName RepNotifyFunc;
    public ELifetimeCondition BlueprintReplicationCondition;

    public override void Deserialize(FAssetArchive Ar)
    {
        base.Deserialize(Ar);
        ArrayDim = Ar.Read<int>();
        ElementSize = Ar.Read<int>();
        PropertyFlags = Ar.Read<EPropertyFlags>();
        RepIndex = Ar.Read<ushort>();
        RepNotifyFunc = Ar.ReadFName();
        BlueprintReplicationCondition = (ELifetimeCondition) Ar.Read<byte>();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (ArrayDim != 1)
        {
            writer.WritePropertyName("ArrayDim");
            writer.WriteValue(ArrayDim);
        }

        if (ElementSize != 0)
        {
            writer.WritePropertyName("ElementSize");
            writer.WriteValue(ElementSize);
        }

        if (PropertyFlags != 0)
        {
            writer.WritePropertyName("PropertyFlags");
            writer.WriteValue(PropertyFlags.ToStringBitfield());
        }

        if (RepIndex != 0)
        {
            writer.WritePropertyName("RepIndex");
            writer.WriteValue(RepIndex);
        }

        if (!RepNotifyFunc.IsNone)
        {
            writer.WritePropertyName("RepNotifyFunc");
            serializer.Serialize(writer, RepNotifyFunc);
        }

        if (BlueprintReplicationCondition != ELifetimeCondition.COND_None)
        {
            writer.WritePropertyName("BlueprintReplicationCondition");
            writer.WriteValue(BlueprintReplicationCondition.ToString());
        }
    }
}

public class FArrayProperty : FProperty
{
    public FProperty? Inner;

    public override void Deserialize(FAssetArchive Ar)
    {
        base.Deserialize(Ar);
        Inner = (FProperty?) SerializeSingleField(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("Inner");
        serializer.Serialize(writer, Inner);
    }
}

public class FBoolProperty : FProperty
{
    public byte FieldSize;
    public byte ByteOffset;
    public byte ByteMask;
    public byte FieldMask;
    public byte BoolSize;
    public bool bIsNativeBool;

    public override void Deserialize(FAssetArchive Ar)
    {
        base.Deserialize(Ar);
        FieldSize = Ar.Read<byte>();
        ByteOffset = Ar.Read<byte>();
        ByteMask = Ar.Read<byte>();
        FieldMask = Ar.Read<byte>();
        BoolSize = Ar.Read<byte>();
        bIsNativeBool = Ar.ReadFlag();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("FieldSize");
        writer.WriteValue(FieldSize);

        writer.WritePropertyName("ByteOffset");
        writer.WriteValue(ByteOffset);

        writer.WritePropertyName("ByteMask");
        writer.WriteValue(ByteMask);

        writer.WritePropertyName("FieldMask");
        writer.WriteValue(FieldMask);

        writer.WritePropertyName("BoolSize");
        writer.WriteValue(BoolSize);

        writer.WritePropertyName("bIsNativeBool");
        writer.WriteValue(bIsNativeBool);
    }
}

public class FByteProperty : FNumericProperty
{
    public FPackageIndex Enum;

    public override void Deserialize(FAssetArchive Ar)
    {
        base.Deserialize(Ar);
        Enum = new FPackageIndex(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("Enum");
        serializer.Serialize(writer, Enum);
    }
}

public class FClassProperty : FObjectProperty
{
    public FPackageIndex MetaClass;

    public override void Deserialize(FAssetArchive Ar)
    {
        base.Deserialize(Ar);
        MetaClass = new FPackageIndex(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("MetaClass");
        serializer.Serialize(writer, MetaClass);
    }
}

public class FDelegateProperty : FProperty
{
    public FPackageIndex SignatureFunction;

    public override void Deserialize(FAssetArchive Ar)
    {
        base.Deserialize(Ar);
        SignatureFunction = new FPackageIndex(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("SignatureFunction");
        serializer.Serialize(writer, SignatureFunction);
    }
}

public class FEnumProperty : FProperty
{
    public FNumericProperty? UnderlyingProp;
    public FPackageIndex Enum;

    public override void Deserialize(FAssetArchive Ar)
    {
        base.Deserialize(Ar);
        Enum = new FPackageIndex(Ar);
        UnderlyingProp = (FNumericProperty?) SerializeSingleField(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("Enum");
        serializer.Serialize(writer, Enum);

        writer.WritePropertyName("UnderlyingProp");
        serializer.Serialize(writer, UnderlyingProp);
    }
}

public class FFieldPathProperty : FProperty
{
    public FName PropertyClass;

    public override void Deserialize(FAssetArchive Ar)
    {
        base.Deserialize(Ar);
        PropertyClass = Ar.ReadFName();
    }
}

public class FDoubleProperty : FNumericProperty;

public class FFloatProperty : FNumericProperty;

public class FInt16Property : FNumericProperty;

public class FInt64Property : FNumericProperty;

public class FInt8Property : FNumericProperty;

public class FIntProperty : FNumericProperty;

public class FInterfaceProperty : FProperty
{
    public FPackageIndex InterfaceClass;

    public override void Deserialize(FAssetArchive Ar)
    {
        base.Deserialize(Ar);
        InterfaceClass = new FPackageIndex(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("InterfaceClass");
        serializer.Serialize(writer, InterfaceClass);
    }
}

public class FMapProperty : FProperty
{
    public FProperty? KeyProp;
    public FProperty? ValueProp;

    public override void Deserialize(FAssetArchive Ar)
    {
        base.Deserialize(Ar);
        KeyProp = (FProperty?) SerializeSingleField(Ar);
        ValueProp = (FProperty?) SerializeSingleField(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("KeyProp");
        serializer.Serialize(writer, KeyProp);

        writer.WritePropertyName("ValueProp");
        serializer.Serialize(writer, ValueProp);
    }
}

public class FMulticastDelegateProperty : FProperty
{
    public FPackageIndex SignatureFunction;

    public override void Deserialize(FAssetArchive Ar)
    {
        base.Deserialize(Ar);
        SignatureFunction = new FPackageIndex(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("SignatureFunction");
        serializer.Serialize(writer, SignatureFunction);
    }
}

public class FMulticastInlineDelegateProperty : FProperty
{
    public FPackageIndex SignatureFunction;

    public override void Deserialize(FAssetArchive Ar)
    {
        base.Deserialize(Ar);
        SignatureFunction = new FPackageIndex(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("SignatureFunction");
        serializer.Serialize(writer, SignatureFunction);
    }
}

public class FNameProperty : FProperty;

public class FNumericProperty : FProperty;

public class FObjectProperty : FProperty
{
    public FPackageIndex PropertyClass;

    public override void Deserialize(FAssetArchive Ar)
    {
        base.Deserialize(Ar);
        PropertyClass = new FPackageIndex(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("PropertyClass");
        serializer.Serialize(writer, PropertyClass);
    }
}

public class FSoftClassProperty : FObjectProperty
{
    public FPackageIndex MetaClass;

    public override void Deserialize(FAssetArchive Ar)
    {
        base.Deserialize(Ar);
        MetaClass = new FPackageIndex(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("MetaClass");
        serializer.Serialize(writer, MetaClass);
    }
}

public class FSoftObjectProperty : FObjectProperty;

public class FSetProperty : FProperty
{
    public FProperty? ElementProp;

    public override void Deserialize(FAssetArchive Ar)
    {
        base.Deserialize(Ar);
        ElementProp = (FProperty?) SerializeSingleField(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("ElementProp");
        serializer.Serialize(writer, ElementProp);
    }
}

public class FStrProperty : FProperty;

public class FStructProperty : FProperty
{
    public FPackageIndex Struct;

    public override void Deserialize(FAssetArchive Ar)
    {
        base.Deserialize(Ar);
        Struct = new FPackageIndex(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("Struct");
        serializer.Serialize(writer, Struct);
    }
}

public class FTextProperty : FProperty;

public class FUInt16Property : FNumericProperty;

public class FUInt32Property : FNumericProperty;

public class FUInt64Property : FNumericProperty;

public class FWeakObjectProperty : FObjectProperty;

public class FOptionalProperty : FProperty
{
    public FProperty? ValueProperty;

    public override void Deserialize(FAssetArchive Ar)
    {
        base.Deserialize(Ar);
        ValueProperty = (FProperty?) SerializeSingleField(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("ValueProperty");
        serializer.Serialize(writer, ValueProperty);
    }
}

public class FVerseStringProperty : FProperty
{
    public FProperty? ValueProperty;

    public override void Deserialize(FAssetArchive Ar)
    {
        base.Deserialize(Ar);
        ValueProperty = (FProperty?) SerializeSingleField(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("ValueProperty");
        serializer.Serialize(writer, ValueProperty);
    }
}

public class FVerseFunctionProperty : FProperty
{
    public FPackageIndex Function;

    public override void Deserialize(FAssetArchive Ar)
    {
        base.Deserialize(Ar);
        Function = new FPackageIndex(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("Struct");
        serializer.Serialize(writer, Function);
    }
}

public class FVerseDynamicProperty : FProperty;
