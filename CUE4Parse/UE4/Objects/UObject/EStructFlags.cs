using System;

namespace CUE4Parse.UE4.Objects.UObject;

[Flags]
public enum EStructFlags : uint
{
    // State flags.
    STRUCT_NoFlags = 0x00000000,
    STRUCT_Native = 0x00000001,

    /** If set, this struct will be compared using native code */
    STRUCT_IdenticalNative = 0x00000002,

    STRUCT_HasInstancedReference = 0x00000004,

    STRUCT_NoExport = 0x00000008,

    /** Indicates that this struct should always be serialized as a single unit */
    STRUCT_Atomic = 0x00000010,

    /** Indicates that this struct uses binary serialization; it is unsafe to add/remove members from this struct without incrementing the package version */
    STRUCT_Immutable = 0x00000020,

    /** If set, native code needs to be run to find referenced objects */
    STRUCT_AddStructReferencedObjects = 0x00000040,

    /** Indicates that this struct should be exportable/importable at the DLL layer.  Base structs must also be exportable for this to work. */
    STRUCT_RequiredAPI = 0x00000200,

    /** If set, this struct will be serialized using the CPP net serializer */
    STRUCT_NetSerializeNative = 0x00000400,

    /** If set, this struct will be serialized using the CPP serializer */
    STRUCT_SerializeNative = 0x00000800,

    /** If set, this struct will be copied using the CPP operator= */
    STRUCT_CopyNative = 0x00001000,

    /** If set, this struct will be copied using memcpy */
    STRUCT_IsPlainOldData = 0x00002000,

    /** If set, this struct has no destructor and non will be called. STRUCT_IsPlainOldData implies STRUCT_NoDestructor */
    STRUCT_NoDestructor = 0x00004000,

    /** If set, this struct will not be constructed because it is assumed that memory is zero before construction. */
    STRUCT_ZeroConstructor = 0x00008000,

    /** If set, native code will be used to export text */
    STRUCT_ExportTextItemNative = 0x00010000,

    /** If set, native code will be used to export text */
    STRUCT_ImportTextItemNative = 0x00020000,

    /** If set, this struct will have PostSerialize called on it after CPP serializer or tagged property serialization is complete */
    STRUCT_PostSerializeNative = 0x00040000,

    /** If set, this struct will have SerializeFromMismatchedTag called on it if a mismatched tag is encountered. */
    STRUCT_SerializeFromMismatchedTag = 0x00080000,

    /** If set, this struct will be serialized using the CPP net delta serializer */
    STRUCT_NetDeltaSerializeNative = 0x00100000,

    /** If set, this struct will be have PostScriptConstruct called on it after a temporary object is constructed in a running blueprint */
    STRUCT_PostScriptConstruct = 0x00200000,

    /** If set, this struct can share net serialization state across connections */
    STRUCT_NetSharedSerialization = 0x00400000,

    /** If set, this struct has been cleaned and sanitized (trashed) and should not be used */
    STRUCT_Trashed = 0x00800000,

    /** If set, this structure has been replaced via reinstancing */
    STRUCT_NewerVersionExists = 0x01000000,

    /** If set, this struct will have CanEditChange on it in the editor to determine if a child property can be edited */
    STRUCT_CanEditChange = 0x02000000,

    /** If set, this struct will have Visit on it to allow custom property visiting implementation */
    STRUCT_Visitor = 0x04000000,

    /** Struct flags that are automatically inherited */
    STRUCT_Inherit = STRUCT_HasInstancedReference | STRUCT_Atomic,

    /** Flags that are always computed, never loaded or done with code generation */
    STRUCT_ComputedFlags = STRUCT_NetDeltaSerializeNative | STRUCT_NetSerializeNative | STRUCT_SerializeNative | STRUCT_PostSerializeNative | STRUCT_CopyNative | STRUCT_IsPlainOldData | STRUCT_NoDestructor | STRUCT_ZeroConstructor | STRUCT_IdenticalNative | STRUCT_AddStructReferencedObjects | STRUCT_ExportTextItemNative | STRUCT_ImportTextItemNative | STRUCT_SerializeFromMismatchedTag | STRUCT_PostScriptConstruct | STRUCT_NetSharedSerialization
};
