namespace CUE4Parse.UE4.Kismet;

public enum EExprToken : byte
{
    // Variable references.
    EX_LocalVariable		= 0x00,	// A local variable.
    EX_InstanceVariable		= 0x01,	// An object variable.
    EX_DefaultVariable		= 0x02, // Default variable for a class context.
    //						= 0x03,
    EX_Return				= 0x04,	// Return from function.
    //						= 0x05,
    EX_Jump					= 0x06,	// Goto a local address in code.
    EX_JumpIfNot			= 0x07,	// Goto if not expression.
    //						= 0x08,
    EX_Assert				= 0x09,	// Assertion.
    //						= 0x0A,
    EX_Nothing				= 0x0B,	// No operation.
    EX_NothingInt32			= 0x0C, // No operation with an int32 argument (useful for debugging script disassembly)
    //						= 0x0D,
    //						= 0x0E,
    EX_Let					= 0x0F,	// Assign an arbitrary size value to a variable.
    //						= 0x10,
    EX_BitFieldConst		= 0x11, // assign to a single bit, defined by an FProperty
    EX_ClassContext			= 0x12,	// Class default object context.
    EX_MetaCast             = 0x13, // Metaclass cast.
    EX_LetBool				= 0x14, // Let boolean variable.
    EX_EndParmValue			= 0x15,	// end of default value for optional function parameter
    EX_EndFunctionParms		= 0x16,	// End of function call parameters.
    EX_Self					= 0x17,	// Self object.
    EX_Skip					= 0x18,	// Skippable expression.
    EX_Context				= 0x19,	// Call a function through an object context.
    EX_Context_FailSilent	= 0x1A, // Call a function through an object context (can fail silently if the context is NULL; only generated for functions that don't have output or return values).
    EX_VirtualFunction		= 0x1B,	// A function call with parameters.
    EX_FinalFunction		= 0x1C,	// A prebound function call with parameters.
    EX_IntConst				= 0x1D,	// Int constant.
    EX_FloatConst			= 0x1E,	// Floating point constant.
    EX_StringConst			= 0x1F,	// String constant.
    EX_ObjectConst		    = 0x20,	// An object constant.
    EX_NameConst			= 0x21,	// A name constant.
    EX_RotationConst		= 0x22,	// A rotation constant.
    EX_VectorConst			= 0x23,	// A vector constant.
    EX_ByteConst			= 0x24,	// A byte constant.
    EX_IntZero				= 0x25,	// Zero.
    EX_IntOne				= 0x26,	// One.
    EX_True					= 0x27,	// Bool True.
    EX_False				= 0x28,	// Bool False.
    EX_TextConst			= 0x29, // FText constant
    EX_NoObject				= 0x2A,	// NoObject.
    EX_TransformConst		= 0x2B, // A transform constant
    EX_IntConstByte			= 0x2C,	// Int constant that requires 1 byte.
    EX_NoInterface			= 0x2D, // A null interface (similar to EX_NoObject, but for interfaces)
    EX_DynamicCast			= 0x2E,	// Safe dynamic class casting.
    EX_StructConst			= 0x2F, // An arbitrary UStruct constant
    EX_EndStructConst		= 0x30, // End of UStruct constant
    EX_SetArray				= 0x31, // Set the value of arbitrary array
    EX_EndArray				= 0x32,
    EX_PropertyConst		= 0x33, // FProperty constant.
    EX_UnicodeStringConst   = 0x34, // Unicode string constant.
    EX_Int64Const			= 0x35,	// 64-bit integer constant.
    EX_UInt64Const			= 0x36,	// 64-bit unsigned integer constant.
    EX_DoubleConst			= 0x37, // Double constant.
    EX_Cast					= 0x38,	// A casting operator which reads the type as the subsequent byte
    EX_SetSet				= 0x39,
    EX_EndSet				= 0x3A,
    EX_SetMap				= 0x3B,
    EX_EndMap				= 0x3C,
    EX_SetConst				= 0x3D,
    EX_EndSetConst			= 0x3E,
    EX_MapConst				= 0x3F,
    EX_EndMapConst			= 0x40,
    EX_Vector3fConst		= 0x41,	// A float vector constant.
    EX_StructMemberContext	= 0x42, // Context expression to address a property within a struct
    EX_LetMulticastDelegate	= 0x43, // Assignment to a multi-cast delegate
    EX_LetDelegate			= 0x44, // Assignment to a delegate
    EX_LocalVirtualFunction	= 0x45, // Special instructions to quickly call a virtual function that we know is going to run only locally
    EX_LocalFinalFunction	= 0x46, // Special instructions to quickly call a final function that we know is going to run only locally
    //						= 0x47, // CST_ObjectToBool
    EX_LocalOutVariable		= 0x48, // local out (pass by reference) function parameter
    //						= 0x49, // CST_InterfaceToBool
    EX_DeprecatedOp4A		= 0x4A,
    EX_InstanceDelegate		= 0x4B,	// const reference to a delegate or normal function object
    EX_PushExecutionFlow	= 0x4C, // push an address on to the execution flow stack for future execution when a EX_PopExecutionFlow is executed.   Execution continues on normally and doesn't change to the pushed address.
    EX_PopExecutionFlow		= 0x4D, // continue execution at the last address previously pushed onto the execution flow stack.
    EX_ComputedJump			= 0x4E,	// Goto a local address in code, specified by an integer value.
    EX_PopExecutionFlowIfNot = 0x4F, // continue execution at the last address previously pushed onto the execution flow stack, if the condition is not true.
    EX_Breakpoint			= 0x50, // Breakpoint.  Only observed in the editor, otherwise it behaves like EX_Nothing.
    EX_InterfaceContext		= 0x51,	// Call a function through a native interface variable
    EX_ObjToInterfaceCast   = 0x52,	// Converting an object reference to native interface variable
    EX_EndOfScript			= 0x53, // Last byte in script code
    EX_CrossInterfaceCast	= 0x54, // Converting an interface variable reference to native interface variable
    EX_InterfaceToObjCast   = 0x55, // Converting an interface variable reference to an object
    //						= 0x56,
    //						= 0x57,
    //						= 0x58,
    //						= 0x59,
    EX_WireTracepoint		= 0x5A, // Trace point.  Only observed in the editor, otherwise it behaves like EX_Nothing.
    EX_SkipOffsetConst		= 0x5B, // A CodeSizeSkipOffset constant
    EX_AddMulticastDelegate = 0x5C, // Adds a delegate to a multicast delegate's targets
    EX_ClearMulticastDelegate = 0x5D, // Clears all delegates in a multicast target
    EX_Tracepoint			= 0x5E, // Trace point.  Only observed in the editor, otherwise it behaves like EX_Nothing.
    EX_LetObj				= 0x5F,	// assign to any object ref pointer
    EX_LetWeakObjPtr		= 0x60, // assign to a weak object pointer
    EX_BindDelegate			= 0x61, // bind object and name to delegate
    EX_RemoveMulticastDelegate = 0x62, // Remove a delegate from a multicast delegate's targets
    EX_CallMulticastDelegate = 0x63, // Call multicast delegate
    EX_LetValueOnPersistentFrame = 0x64,
    EX_ArrayConst			= 0x65,
    EX_EndArrayConst		= 0x66,
    EX_SoftObjectConst		= 0x67,
    EX_CallMath				= 0x68, // static pure function from on local call space
    EX_SwitchValue			= 0x69,
    EX_InstrumentationEvent	= 0x6A, // Instrumentation event
    EX_ArrayGetByRef		= 0x6B,
    EX_ClassSparseDataVariable = 0x6C, // Sparse data variable
    EX_FieldPathConst		= 0x6D,
    //						= 0x6E,
    //						= 0x6F,
    EX_AutoRtfmTransact     = 0x70, // AutoRTFM: run following code in a transaction
    EX_AutoRtfmStopTransact = 0x71, // AutoRTFM: if in a transaction, abort or break, otherwise no operation
    EX_AutoRtfmAbortIfNot   = 0x72, // AutoRTFM: evaluate bool condition, abort transaction on false
    EX_Max					= 0xFF,
};

public enum ECastToken : byte
{
    CST_ObjectToInterface		= 0x00,
    CST_ObjectToBool			= 0x01,//idk if this is used or 0x47 is used
    CST_InterfaceToBool			= 0x02,
    CST_DoubleToFloat			= 0x03,
    CST_FloatToDouble			= 0x04,
    CST_ObjectToBool2           = 0x47,
    CST_InterfaceToBool2        = 0x49,

    CST_Max						= 0xFF,
};

public enum EScriptInstrumentationType : byte
{
    Class = 0,
    ClassScope,
    Instance,
    Event,
    InlineEvent,
    ResumeEvent,
    PureNodeEntry,
    NodeDebugSite,
    NodeEntry,
    NodeExit,
    PushState,
    RestoreState,
    ResetState,
    SuspendState,
    PopState,
    TunnelEndOfThread,
    Stop
}

public enum EBlueprintTextLiteralType : byte
{
    /* Text is an empty string. The bytecode contains no strings, and you should use FText::GetEmpty() to initialize the FText instance. */
    Empty,
    /** Text is localized. The bytecode will contain three strings - source, key, and namespace - and should be loaded via FInternationalization */
    LocalizedText,
    /** Text is culture invariant. The bytecode will contain one string, and you should use FText::AsCultureInvariant to initialize the FText instance. */
    InvariantText,
    /** Text is a literal FString. The bytecode will contain one string, and you should use FText::FromString to initialize the FText instance. */
    LiteralString,
    /** Text is from a string table. The bytecode will contain an object pointer (not used) and two strings - the table ID, and key - and should be found via FText::FromStringTable */
    StringTableEntry,
};

public enum EAutoRtfmStopTransactMode : byte
{
    GracefulExit,
    AbortingExit,
    AbortingExitAndAbortParent,
};
