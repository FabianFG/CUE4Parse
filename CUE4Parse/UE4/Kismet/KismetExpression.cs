using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Versions;
using System.Text;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Kismet;

[JsonConverter(typeof(FKismetPropertyPointerConverter))]
public class FKismetPropertyPointer
{
    public bool bNew { get; } = true;
    public FPackageIndex? Old;
    public FFieldPath? New;

    public FKismetPropertyPointer(FKismetArchive Ar)
    {
        if (Ar.Game >= EGame.GAME_UE4_25)
        {
            New = new FFieldPath(Ar);
        }
        else
        {
            bNew = false;
            Old = new FPackageIndex(Ar);
        }
    }
}

[JsonConverter(typeof(KismetExpressionConverter))]
public abstract class KismetExpression
{
    public virtual EExprToken Token => EExprToken.EX_Nothing;
    public int StatementIndex = 0;

    protected internal virtual void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        writer.WritePropertyName("Inst");
        writer.WriteValue(Token.ToString());

        if (bAddIndex)
        {
            writer.WritePropertyName("StatementIndex");
            writer.WriteValue(StatementIndex);
        }
    }
}

public abstract class KismetExpression<T> : KismetExpression
{
    public T Value;

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("Value");
        serializer.Serialize(writer, Value);
    }
}

public class EX_AddMulticastDelegate : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_AddMulticastDelegate;
    public KismetExpression Delegate;
    public KismetExpression DelegateToAdd;

    public EX_AddMulticastDelegate(FKismetArchive Ar)
    {
        Delegate = Ar.ReadExpression();
        DelegateToAdd = Ar.ReadExpression();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("MulticastDelegate");
        serializer.Serialize(writer, Delegate);
        writer.WritePropertyName("Delegate");
        serializer.Serialize(writer, DelegateToAdd);
    }
}

public class EX_ArrayConst : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_ArrayConst;
    public FKismetPropertyPointer InnerProperty;
    public KismetExpression[] Elements;

    public EX_ArrayConst(FKismetArchive Ar)
    {
        InnerProperty = new FKismetPropertyPointer(Ar);
        int numEntries = Ar.Read<int>(); // Number of elements
        Elements = Ar.ReadExpressionArray(EExprToken.EX_EndArrayConst);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("InnerProperty");
        serializer.Serialize(writer, InnerProperty);
        writer.WritePropertyName("Values");
        serializer.Serialize(writer, Elements);
    }
}

public class EX_ArrayGetByRef : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_ArrayGetByRef;
    public KismetExpression ArrayVariable;
    public KismetExpression ArrayIndex;

    public EX_ArrayGetByRef(FKismetArchive Ar)
    {
        ArrayVariable = Ar.ReadExpression();
        ArrayIndex = Ar.ReadExpression();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("ArrayVariable");
        serializer.Serialize(writer, ArrayVariable);
        writer.WritePropertyName("ArrayIndex");
        serializer.Serialize(writer, ArrayIndex);
    }
}

public class EX_Assert : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_Assert;
    public ushort LineNumber;
    public bool DebugMode;
    public KismetExpression AssertExpression;

    public EX_Assert(FKismetArchive Ar)
    {
        LineNumber = Ar.Read<ushort>();
        DebugMode = Ar.ReadFlag();
        AssertExpression = Ar.ReadExpression();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("LineNumber");
        writer.WriteValue(LineNumber);
        writer.WritePropertyName("DebugMode");
        writer.WriteValue(DebugMode);
        writer.WritePropertyName("AssertExpression");
        serializer.Serialize(writer, AssertExpression);
    }
}

public class EX_BindDelegate : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_BindDelegate;
    public FName FunctionName;
    public KismetExpression Delegate;
    public KismetExpression ObjectTerm;

    public EX_BindDelegate(FKismetArchive Ar)
    {
        FunctionName = Ar.ReadFName();
        Delegate = Ar.ReadExpression();
        ObjectTerm = Ar.ReadExpression();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("FunctionName");
        serializer.Serialize(writer, FunctionName);
        writer.WritePropertyName("Delegate");
        serializer.Serialize(writer, Delegate);
        writer.WritePropertyName("ObjectTerm");
        serializer.Serialize(writer, ObjectTerm);
    }
}

public class EX_Breakpoint : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_Breakpoint;
}

public class EX_ByteConst : KismetExpression<byte>
{
    public override EExprToken Token => EExprToken.EX_ByteConst;

    public EX_ByteConst(FKismetArchive Ar)
    {
        Value = Ar.Read<byte>();
    }
}

public class EX_CallMath : EX_FinalFunction
{
    public override EExprToken Token => EExprToken.EX_CallMath;

    public EX_CallMath(FKismetArchive Ar) : base(Ar) { }
}

public class EX_CallMulticastDelegate : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_CallMulticastDelegate;
    public FPackageIndex StackNode;
    public KismetExpression Delegate;
    public KismetExpression[] Parameters;

    public EX_CallMulticastDelegate(FKismetArchive Ar)
    {
        StackNode = new FPackageIndex(Ar);
        Delegate = Ar.ReadExpression();
        Parameters = Ar.ReadExpressionArray(EExprToken.EX_EndFunctionParms);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("FunctionName");
        serializer.Serialize(writer, StackNode);
        writer.WritePropertyName("Delegate");
        serializer.Serialize(writer, Delegate);
        writer.WritePropertyName("Parameters");
        serializer.Serialize(writer, Parameters);
    }
}

public class EX_Cast : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_Cast;// EX_PrimitiveCast
    public ECastToken ConversionType;
    public KismetExpression Target;

    public EX_Cast(FKismetArchive Ar)
    {
        ConversionType = (ECastToken)Ar.Read<byte>();
        Target = Ar.ReadExpression();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("ConversionType");
        writer.WriteValue(ConversionType.ToString());
        writer.WritePropertyName("Target");
        serializer.Serialize(writer, Target);
    }
}

public abstract class EX_CastBase : KismetExpression
{
    public FPackageIndex ClassPtr;
    public KismetExpression Target;

    public EX_CastBase(FKismetArchive Ar)
    {
        ClassPtr = new FPackageIndex(Ar);
        Target = Ar.ReadExpression();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("InterfaceClass");
        serializer.Serialize(writer, ClassPtr);
        writer.WritePropertyName("Target");
        serializer.Serialize(writer, Target);
    }
}

public class EX_ClassContext : EX_Context
{
    public override EExprToken Token => EExprToken.EX_ClassContext;

    public EX_ClassContext(FKismetArchive Ar) : base(Ar) { }
}

public class EX_BitFieldConst : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_BitFieldConst;
    public FKismetPropertyPointer InnerProperty;
    public byte ConstValue;

    public EX_BitFieldConst(FKismetArchive Ar)
    {
        InnerProperty = new FKismetPropertyPointer(Ar);
        ConstValue = Ar.Read<byte>();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("InnerProperty");
        serializer.Serialize(writer, InnerProperty);
        writer.WritePropertyName("ConstValue");
        serializer.Serialize(writer, ConstValue);
    }
}

public class EX_ClassSparseDataVariable : EX_VariableBase
{
    public override EExprToken Token => EExprToken.EX_ClassSparseDataVariable;

    public EX_ClassSparseDataVariable(FKismetArchive Ar) : base(Ar) { }
}

public class EX_ClearMulticastDelegate : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_ClearMulticastDelegate;
    public KismetExpression DelegateToClear;

    public EX_ClearMulticastDelegate(FKismetArchive Ar)
    {
        DelegateToClear = Ar.ReadExpression();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("DelegateToClear");
        serializer.Serialize(writer, DelegateToClear);
    }
}

public class EX_ComputedJump : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_ComputedJump;
    public KismetExpression CodeOffsetExpression;

    public EX_ComputedJump(FKismetArchive Ar)
    {
        CodeOffsetExpression = Ar.ReadExpression();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("OffsetExpression");
        serializer.Serialize(writer, CodeOffsetExpression);
    }
}

public class EX_Context : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_Context;
    public KismetExpression ObjectExpression;
    public uint Offset;
    public FKismetPropertyPointer RValuePointer;
    public KismetExpression ContextExpression;

    public EX_Context(FKismetArchive Ar)
    {
        ObjectExpression = Ar.ReadExpression();
        Offset = Ar.Read<uint>();
        RValuePointer = new FKismetPropertyPointer(Ar);
        ContextExpression = Ar.ReadExpression();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("ObjectExpression");
        serializer.Serialize(writer, ObjectExpression);
        writer.WritePropertyName("Offset");
        writer.WriteValue(Offset);
        writer.WritePropertyName("RValuePointer");
        serializer.Serialize(writer, RValuePointer);
        writer.WritePropertyName("ContextExpression");
        serializer.Serialize(writer, ContextExpression);
    }
}

public class EX_Context_FailSilent : EX_Context
{
    public override EExprToken Token => EExprToken.EX_Context_FailSilent;

    public EX_Context_FailSilent(FKismetArchive Ar) : base(Ar) { }
}

public class EX_CrossInterfaceCast : EX_CastBase
{
    public override EExprToken Token => EExprToken.EX_CrossInterfaceCast;

    public EX_CrossInterfaceCast(FKismetArchive Ar) : base(Ar) { }
}

public class EX_DefaultVariable : EX_VariableBase
{
    public override EExprToken Token => EExprToken.EX_DefaultVariable;

    public EX_DefaultVariable(FKismetArchive Ar) : base(Ar) { }
}

public class EX_DeprecatedOp4A : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_DeprecatedOp4A;
}

public class EX_DoubleConst : KismetExpression<double>
{
    public override EExprToken Token => EExprToken.EX_DoubleConst;

    public EX_DoubleConst(FKismetArchive Ar)
    {
        Value = Ar.Read<double>();
    }
}

public class EX_DynamicCast : EX_CastBase
{
    public override EExprToken Token => EExprToken.EX_DynamicCast;

    public EX_DynamicCast(FKismetArchive Ar) : base(Ar) { }
}

public class EX_EndArray : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_EndArray;
}

public class EX_EndArrayConst : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_EndArrayConst;
}

public class EX_EndFunctionParms : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_EndFunctionParms;
}

public class EX_EndMap : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_EndMap;
}

public class EX_EndMapConst : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_EndMapConst;
}

public class EX_EndOfScript : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_EndOfScript;
}

public class EX_EndParmValue : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_EndParmValue;
}
public class EX_EndSet : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_EndSet;
}
public class EX_EndSetConst : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_EndSetConst;
}
public class EX_EndStructConst : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_EndStructConst;
}

public class EX_False : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_False;
}

public class EX_FieldPathConst : KismetExpression<KismetExpression>
{
    public override EExprToken Token => EExprToken.EX_FieldPathConst;

    public EX_FieldPathConst(FKismetArchive Ar)
    {
        Value = Ar.ReadExpression();
    }
}

public class EX_FinalFunction : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_FinalFunction;
    public FPackageIndex StackNode;
    public KismetExpression[] Parameters;

    public EX_FinalFunction(FKismetArchive Ar)
    {
        StackNode = new FPackageIndex(Ar);
        Parameters = Ar.ReadExpressionArray(EExprToken.EX_EndFunctionParms);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("Function");
        serializer.Serialize(writer, StackNode);
        writer.WritePropertyName("Parameters");
        serializer.Serialize(writer, Parameters);

        if (Parameters.Length == 1 && Parameters[0] is EX_IntConst offsetint)
        {
            if (StackNode.ResolvedObject is not null && StackNode.ResolvedObject.Class?.Name.Text == "Function")
            {
                var ObjectPath = new StringBuilder();
                ObjectPath.Append(StackNode.Owner);
                ObjectPath.Append('.');
                ObjectPath.Append(StackNode.Name);
                ObjectPath.Append('[');
                ObjectPath.Append(offsetint.Value);
                ObjectPath.Append(']');
                writer.WritePropertyName("ObjectPath");
                writer.WriteValue(ObjectPath.ToString());
            }
        }
    }
}

public class EX_FloatConst : KismetExpression<float>
{
    public override EExprToken Token => EExprToken.EX_FloatConst;

    public EX_FloatConst(FKismetArchive Ar)
    {
        Value = Ar.Read<float>();
    }
}

public class EX_InstanceDelegate : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_InstanceDelegate;
    public FName FunctionName;

    public EX_InstanceDelegate(FKismetArchive Ar)
    {
        FunctionName = Ar.ReadFName();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("FunctionName");
        serializer.Serialize(writer, FunctionName);
    }
}

public class EX_InstanceVariable : EX_VariableBase
{
    public override EExprToken Token => EExprToken.EX_InstanceVariable;

    public EX_InstanceVariable(FKismetArchive Ar) : base(Ar) { }
}

public class EX_InstrumentationEvent : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_InstrumentationEvent;

    public EScriptInstrumentationType EventType;
    public FName? EventName;

    public EX_InstrumentationEvent(FKismetArchive Ar)
    {
        EventType = (EScriptInstrumentationType)Ar.Read<byte>();

        if (EventType.Equals(EScriptInstrumentationType.InlineEvent))
        {
            EventName = Ar.ReadFName();
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        if (EventType.Equals(EScriptInstrumentationType.InlineEvent))
        {
            writer.WritePropertyName("EventName");
            serializer.Serialize(writer, EventName);
        }
    }
}

public class EX_Int64Const : KismetExpression<long>
{
    public override EExprToken Token => EExprToken.EX_Int64Const;

    public EX_Int64Const(FKismetArchive Ar)
    {
        Value = Ar.Read<long>();
    }
}

public class EX_IntConst : KismetExpression<int>
{
    public override EExprToken Token => EExprToken.EX_IntConst;

    public EX_IntConst(FKismetArchive Ar)
    {
        Value = Ar.Read<int>();
    }
}

public class EX_IntConstByte : KismetExpression<byte>
{
    public override EExprToken Token => EExprToken.EX_IntConstByte;

    public EX_IntConstByte(FKismetArchive Ar)
    {
        Value = Ar.Read<byte>();
    }
}

public class EX_IntOne : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_IntOne;
}

public class EX_IntZero : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_IntZero;
}

public class EX_InterfaceContext : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_InterfaceContext;
    public KismetExpression InterfaceValue;

    public EX_InterfaceContext(FKismetArchive Ar)
    {
        InterfaceValue = Ar.ReadExpression();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("InterfaceValue");
        serializer.Serialize(writer, InterfaceValue);
    }
}

public class EX_InterfaceToObjCast : EX_CastBase
{
    public override EExprToken Token => EExprToken.EX_InterfaceToObjCast;

    public EX_InterfaceToObjCast(FKismetArchive Ar) : base(Ar) { }
}

public class EX_Jump : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_Jump;
    public uint CodeOffset;
    public StringBuilder ObjectPath = new();

    public EX_Jump(FKismetArchive Ar)
    {
        CodeOffset = Ar.Read<uint>();
        ObjectPath.Append(Ar.Owner.Name);
        ObjectPath.Append('.');
        ObjectPath.Append(Ar.Name);
        ObjectPath.Append('[');
        ObjectPath.Append(CodeOffset);
        ObjectPath.Append(']');
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("CodeOffset");
        writer.WriteValue(CodeOffset);
        writer.WritePropertyName("ObjectPath");
        writer.WriteValue(ObjectPath.ToString());
    }
}

public class EX_JumpIfNot : EX_Jump
{
    public override EExprToken Token => EExprToken.EX_JumpIfNot;
    public KismetExpression BooleanExpression;

    public EX_JumpIfNot(FKismetArchive Ar) : base(Ar)
    {
        BooleanExpression = Ar.ReadExpression();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("BooleanExpression");
        serializer.Serialize(writer, BooleanExpression);
    }
}
public class EX_Let : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_Let;
    public FKismetPropertyPointer Property;
    public KismetExpression Variable;
    public KismetExpression Assignment;

    public EX_Let(FKismetArchive Ar)
    {
        Property = new FKismetPropertyPointer(Ar);
        Variable = Ar.ReadExpression();
        Assignment = Ar.ReadExpression();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        //writer.WritePropertyName("Property");
        //serializer.Serialize(writer, Property);
        writer.WritePropertyName("Variable");
        serializer.Serialize(writer, Variable);
        writer.WritePropertyName("Expression");
        serializer.Serialize(writer, Assignment);
    }
}

public abstract class EX_LetBase : KismetExpression
{
    public KismetExpression Variable;
    public KismetExpression Assignment;

    public EX_LetBase(FKismetArchive Ar)
    {
        Variable = Ar.ReadExpression();
        Assignment = Ar.ReadExpression();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("Variable");
        serializer.Serialize(writer, Variable);
        writer.WritePropertyName("Expression");
        serializer.Serialize(writer, Assignment);
    }
}

public class EX_LetBool : EX_LetBase
{
    public override EExprToken Token => EExprToken.EX_LetBool;

    public EX_LetBool(FKismetArchive Ar) : base(Ar) { }
}

public class EX_LetDelegate : EX_LetBase
{
    public override EExprToken Token => EExprToken.EX_LetDelegate;

    public EX_LetDelegate(FKismetArchive Ar) : base(Ar) { }
}

public class EX_LetMulticastDelegate : EX_LetBase
{
    public override EExprToken Token => EExprToken.EX_LetMulticastDelegate;

    public EX_LetMulticastDelegate (FKismetArchive Ar) : base(Ar) { }
}

public class EX_LetObj : EX_LetBase
{
    public override EExprToken Token => EExprToken.EX_LetObj;

    public EX_LetObj(FKismetArchive Ar) : base(Ar) { }
}

public class EX_LetValueOnPersistentFrame : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_LetValueOnPersistentFrame;
    public FKismetPropertyPointer DestinationProperty;
    public KismetExpression AssignmentExpression;
    public EX_LetValueOnPersistentFrame(FKismetArchive Ar)
    {
        DestinationProperty = new FKismetPropertyPointer(Ar);
        AssignmentExpression = Ar.ReadExpression();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("DestinationProperty");
        serializer.Serialize(writer, DestinationProperty);
        writer.WritePropertyName("AssignmentExpression");
        serializer.Serialize(writer, AssignmentExpression);
    }
}

public class EX_LetWeakObjPtr : EX_LetBase
{
    public override EExprToken Token => EExprToken.EX_LetWeakObjPtr;

    public EX_LetWeakObjPtr (FKismetArchive Ar) : base(Ar) { }
}

public class EX_LocalFinalFunction : EX_FinalFunction
{
    public override EExprToken Token => EExprToken.EX_LocalFinalFunction;

    public EX_LocalFinalFunction(FKismetArchive Ar) : base(Ar) { }
}

public class EX_LocalOutVariable : EX_VariableBase
{
    public override EExprToken Token => EExprToken.EX_LocalOutVariable;

    public EX_LocalOutVariable(FKismetArchive Ar) : base(Ar) { }
}

public class EX_LocalVariable : EX_VariableBase
{
    public override EExprToken Token => EExprToken.EX_LocalVariable;

    public EX_LocalVariable(FKismetArchive Ar) : base(Ar) { }
}

public class EX_LocalVirtualFunction  : EX_VirtualFunction
{
    public override EExprToken Token => EExprToken.EX_LocalVirtualFunction ;

    public EX_LocalVirtualFunction (FKismetArchive Ar) : base(Ar) { }
}

public class EX_MapConst : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_MapConst;
    public FKismetPropertyPointer KeyProperty;
    public FKismetPropertyPointer ValueProperty;
    public KismetExpression[] Elements;

    public EX_MapConst(FKismetArchive Ar)
    {
        KeyProperty = new FKismetPropertyPointer(Ar);
        ValueProperty = new FKismetPropertyPointer(Ar);
        int numEntries = Ar.Read<int>(); // Number of elements
        Elements = Ar.ReadExpressionArray(EExprToken.EX_EndMapConst);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("KeyProperty");
        serializer.Serialize(writer, KeyProperty);
        writer.WritePropertyName("ValueProperty");
        serializer.Serialize(writer, ValueProperty);

        writer.WritePropertyName("Values");
        writer.WriteStartArray();
        for (var j = 1; j <= Elements.Length / 2; j++)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Key");
            serializer.Serialize(writer, Elements[2 * (j - 1)]);
            writer.WritePropertyName("Value");
            serializer.Serialize(writer, Elements[2 * (j - 1) + 1]);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }
}

public class EX_MetaCast : EX_CastBase
{
    public override EExprToken Token => EExprToken.EX_MetaCast;

    public EX_MetaCast(FKismetArchive Ar) : base(Ar) { }
}

public class EX_NameConst : KismetExpression<FName>
{
    public override EExprToken Token => EExprToken.EX_NameConst;
    public EX_NameConst(FKismetArchive Ar)
    {
        Value = Ar.ReadFName();
    }
}

public class EX_NoInterface : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_NoInterface;
}

public class EX_NoObject : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_NoObject;
}

public class EX_Nothing : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_Nothing;
}

public class EX_NothingInt32 : KismetExpression<int>
{
    public override EExprToken Token => EExprToken.EX_NothingInt32;

    public EX_NothingInt32(FKismetArchive Ar)
    {
        Value = Ar.Read<int>();
    }
}

public class EX_ObjToInterfaceCast : EX_CastBase
{
    public override EExprToken Token => EExprToken.EX_ObjToInterfaceCast;

    public EX_ObjToInterfaceCast(FKismetArchive Ar) : base(Ar) { }
}

public class EX_ObjectConst : KismetExpression<FPackageIndex>
{
    public override EExprToken Token => EExprToken.EX_ObjectConst;
    public EX_ObjectConst(FKismetArchive Ar)
    {
        Value = new FPackageIndex(Ar);
    }
}

public class EX_PopExecutionFlow : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_PopExecutionFlow;
}

public class EX_PopExecutionFlowIfNot : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_PopExecutionFlowIfNot;
    public KismetExpression BooleanExpression;
    public EX_PopExecutionFlowIfNot(FKismetArchive Ar)
    {
        BooleanExpression = Ar.ReadExpression();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("BooleanExpression");
        serializer.Serialize(writer, BooleanExpression);
    }
}

public class EX_PropertyConst : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_PropertyConst;
    public FKismetPropertyPointer Property;

    public EX_PropertyConst(FKismetArchive Ar)
    {
        Property = new FKismetPropertyPointer(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("Property");
        serializer.Serialize(writer, Property);
    }
}

public class EX_PushExecutionFlow : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_PushExecutionFlow;
    public uint PushingAddress;
    public StringBuilder ObjectPath = new();

    public EX_PushExecutionFlow(FKismetArchive Ar)
    {
        PushingAddress = Ar.Read<uint>();
        ObjectPath.Append(Ar.Owner.Name);
        ObjectPath.Append('.');
        ObjectPath.Append(Ar.Name);
        ObjectPath.Append('[');
        ObjectPath.Append(PushingAddress);
        ObjectPath.Append(']');
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("PushingAddress");
        writer.WriteValue(PushingAddress);
        writer.WritePropertyName("ObjectPath");
        writer.WriteValue(ObjectPath.ToString());
    }
}

public class EX_RemoveMulticastDelegate : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_RemoveMulticastDelegate;
    public KismetExpression Delegate;
    public KismetExpression DelegateToAdd;

    public EX_RemoveMulticastDelegate(FKismetArchive Ar)
    {
        Delegate = Ar.ReadExpression();
        DelegateToAdd = Ar.ReadExpression();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("MulticastDelegate");
        serializer.Serialize(writer, Delegate);
        writer.WritePropertyName("Delegate");
        serializer.Serialize(writer, DelegateToAdd);
    }
}

public class EX_Return : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_Return;
    public KismetExpression ReturnExpression;

    public EX_Return(FKismetArchive Ar)
    {
        ReturnExpression = Ar.ReadExpression();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("Expression");
        serializer.Serialize(writer, ReturnExpression);
    }
}

public class EX_RotationConst : KismetExpression<FRotator>
{
    public override EExprToken Token => EExprToken.EX_RotationConst;

    public EX_RotationConst(FKismetArchive Ar)
    {
        Value = new FRotator(Ar);
    }
}

public class EX_Self : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_Self;
}

public class EX_SetArray : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_SetArray;
    public KismetExpression? AssigningProperty;
    public FPackageIndex? ArrayInnerProp;
    public KismetExpression[] Elements;

    public EX_SetArray(FKismetArchive Ar)
    {
        if (Ar.Ver >= EUnrealEngineObjectUE4Version.CHANGE_SETARRAY_BYTECODE)
        {
            AssigningProperty = Ar.ReadExpression();
        }
        else
        {
            ArrayInnerProp = new FPackageIndex(Ar);
        }

        Elements = Ar.ReadExpressionArray(EExprToken.EX_EndArray);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        if (AssigningProperty is not null)
        {
            writer.WritePropertyName("AssigningProperty");
            serializer.Serialize(writer, AssigningProperty);
        }
        else
        {
            writer.WritePropertyName("ArrayInnerProp");
            serializer.Serialize(writer, ArrayInnerProp);
        }
        writer.WritePropertyName("Elements");
        serializer.Serialize(writer, Elements);
    }
}

public class EX_SetConst : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_SetConst;
    public FKismetPropertyPointer InnerProperty;
    public KismetExpression[] Elements;

    public EX_SetConst(FKismetArchive Ar)
    {
        InnerProperty = new FKismetPropertyPointer(Ar);
        int numEntries = Ar.Read<int>(); // Number of elements
        Elements = Ar.ReadExpressionArray(EExprToken.EX_EndSetConst);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("InnerProperty");
        serializer.Serialize(writer, InnerProperty);
        writer.WritePropertyName("Elements");
        serializer.Serialize(writer, Elements);
    }
}

public class EX_SetMap : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_SetMap;
    public KismetExpression MapProperty;
    public KismetExpression[] Elements;

    public EX_SetMap(FKismetArchive Ar)
    {
        MapProperty = Ar.ReadExpression();
        int numEntries = Ar.Read<int>(); // Number of elements
        Elements = Ar.ReadExpressionArray(EExprToken.EX_EndMap);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("MapProperty");
        serializer.Serialize(writer, MapProperty);
        writer.WritePropertyName("Values");
        writer.WriteStartArray();
        for (var j = 1; j <= Elements.Length / 2; j++)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Key");
            serializer.Serialize(writer, Elements[2 * (j - 1)]);
            writer.WritePropertyName("Value");
            serializer.Serialize(writer, Elements[2 * (j - 1) + 1]);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }
}

public class EX_SetSet : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_SetSet;
    public KismetExpression SetProperty;
    public KismetExpression[] Elements;

    public EX_SetSet(FKismetArchive Ar)
    {
        SetProperty = Ar.ReadExpression();
        int numEntries = Ar.Read<int>(); // Number of elements
        Elements = Ar.ReadExpressionArray(EExprToken.EX_EndSet);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("SetProperty");
        serializer.Serialize(writer, SetProperty);
        writer.WritePropertyName("Elements");
        serializer.Serialize(writer, Elements);
    }
}

public class EX_Skip : EX_Jump
{
    public override EExprToken Token => EExprToken.EX_Skip;
    public KismetExpression SkipExpression;

    public EX_Skip(FKismetArchive Ar) : base(Ar)
    {
        CodeOffset = Ar.Read<uint>();
        SkipExpression = Ar.ReadExpression();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("SkipExpression");
        serializer.Serialize(writer, SkipExpression);

    }
}

public class EX_SkipOffsetConst : KismetExpression<uint>
{
    public override EExprToken Token => EExprToken.EX_SkipOffsetConst;

    public EX_SkipOffsetConst(FKismetArchive Ar)
    {
        Value = Ar.Read<uint>();
    }
}

public class EX_SoftObjectConst : KismetExpression<KismetExpression>
{
    public override EExprToken Token => EExprToken.EX_SoftObjectConst;

    public EX_SoftObjectConst(FKismetArchive Ar)
    {
        Value = Ar.ReadExpression();
    }
}

public class EX_StringConst : KismetExpression<string>
{
    public override EExprToken Token=> EExprToken.EX_StringConst;
    public EX_StringConst(FKismetArchive Ar)
    {
        Value = Ar.XFERSTRING();
        Ar.Position++;
        Ar.Index++;
    }
}

public class EX_StructConst : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_StructConst;
    public FPackageIndex Struct;
    public int StructSize;
    public KismetExpression[] Properties;

    public EX_StructConst(FKismetArchive Ar)
    {
        Struct = new FPackageIndex(Ar);
        StructSize = Ar.Read<int>();
        Properties = Ar.ReadExpressionArray(EExprToken.EX_EndStructConst);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("Struct");
        serializer.Serialize(writer, Struct);
        writer.WritePropertyName("Properties");
        serializer.Serialize(writer, Properties);
    }
}

public class EX_StructMemberContext : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_StructMemberContext;
    public FKismetPropertyPointer Property;
    public KismetExpression StructExpression;

    public EX_StructMemberContext(FKismetArchive Ar)
    {
        Property = new FKismetPropertyPointer(Ar);
        StructExpression = Ar.ReadExpression();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("Property");
        serializer.Serialize(writer, Property);
        writer.WritePropertyName("StructExpression");
        serializer.Serialize(writer, StructExpression);
    }
}

public struct FKismetSwitchCase
{
    public KismetExpression CaseIndexValueTerm;
    public uint NextOffset;
    public KismetExpression CaseTerm;

    public FKismetSwitchCase(FKismetArchive Ar)
    {
        CaseIndexValueTerm = Ar.ReadExpression();
        NextOffset = Ar.Read<uint>();
        CaseTerm = Ar.ReadExpression();
    }
}

public class EX_SwitchValue : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_SwitchValue;
    public uint EndGotoOffset;
    public KismetExpression IndexTerm;
    public KismetExpression DefaultTerm;
    public FKismetSwitchCase[] Cases;

    public EX_SwitchValue(FKismetArchive Ar)
    {
        ushort numCases = Ar.Read<ushort>(); // number of cases, without default one
        EndGotoOffset = Ar.Read<uint>();
        IndexTerm = Ar.ReadExpression();
        Cases = Ar.ReadArray(numCases, () => new FKismetSwitchCase(Ar));
        DefaultTerm = Ar.ReadExpression();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("IndexTerm");
        serializer.Serialize(writer, IndexTerm);
        writer.WritePropertyName("EndGotoOffset");
        writer.WriteValue(EndGotoOffset);
        writer.WritePropertyName("Cases");
        serializer.Serialize(writer, Cases);
        writer.WritePropertyName("DefaultTerm");
        serializer.Serialize(writer, DefaultTerm);
    }
}

public class EX_TextConst : KismetExpression<FScriptText>
{
    public override EExprToken Token => EExprToken.EX_TextConst;

    public EX_TextConst(FKismetArchive Ar)
    {
        Value = new FScriptText(Ar);
    }
}

public class EX_Tracepoint : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_Tracepoint;
}

public class EX_TransformConst : KismetExpression<FTransform>
{
    public override EExprToken Token => EExprToken.EX_TransformConst;

    public EX_TransformConst(FKismetArchive Ar)
    {
        Value = new FTransform(Ar);
    }
}

public class EX_True : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_True;
}

public class EX_UInt64Const : KismetExpression<ulong>
{
    public override EExprToken Token => EExprToken.EX_UInt64Const;

    public EX_UInt64Const(FKismetArchive Ar)
    {
        Value = Ar.Read<ulong>();
    }
}

public class EX_UnicodeStringConst : KismetExpression<string>
{
    public override EExprToken Token => EExprToken.EX_UnicodeStringConst;

    public EX_UnicodeStringConst(FKismetArchive Ar)
    {
        Value = Ar.XFERUNICODESTRING();
        Ar.Position += 2;
        Ar.Index += 2;
    }
}

public abstract class EX_VariableBase : KismetExpression
{
    public FKismetPropertyPointer Variable;

    public EX_VariableBase(FKismetArchive Ar)
    {
        Variable = new FKismetPropertyPointer(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("Variable");
        serializer.Serialize(writer, Variable);
    }
}

public class EX_Vector3fConst : KismetExpression<FVector>
{
    public override EExprToken Token => EExprToken.EX_Vector3fConst;

    public EX_Vector3fConst(FKismetArchive Ar)
    {
        Value = Ar.Read<FVector>();
    }
}

public class EX_VectorConst : KismetExpression<FVector>
{
    public override EExprToken Token => EExprToken.EX_VectorConst;

    public EX_VectorConst(FKismetArchive Ar)
    {
        Value = new FVector(Ar);
    }
}

public class EX_VirtualFunction : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_VirtualFunction;
    public FName VirtualFunctionName;
    public KismetExpression[] Parameters;

    public EX_VirtualFunction(FKismetArchive Ar)
    {
        VirtualFunctionName = Ar.ReadFName();
        Parameters = Ar.ReadExpressionArray(EExprToken.EX_EndFunctionParms);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("Function");
        serializer.Serialize(writer, VirtualFunctionName);
        writer.WritePropertyName("Parameters");
        serializer.Serialize(writer, Parameters);
    }
}

public class EX_WireTracepoint : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_WireTracepoint;
}

public class EX_AutoRtfmStopTransact : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_AutoRtfmStopTransact;
    public int Id;
    public EAutoRtfmStopTransactMode Mode;

    public EX_AutoRtfmStopTransact(FKismetArchive Ar)
    {
        Id = Ar.Read<int>();
        Mode = Ar.Read<EAutoRtfmStopTransactMode>();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("Id");
        writer.WriteValue(Id);
        writer.WritePropertyName("Mode");
        writer.WriteValue(Mode);
    }
}

public class EX_AutoRtfmTransact : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_AutoRtfmTransact;
    public int Id;
    public uint CodeOffset;
    public KismetExpression[] Parameters;

    public EX_AutoRtfmTransact(FKismetArchive Ar)
    {
        Id =  Ar.Read<int>();
        CodeOffset =  Ar.Read<uint>();
        Parameters = Ar.ReadExpressionArray(EExprToken.EX_AutoRtfmStopTransact);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("Id");
        writer.WriteValue(Id);
        writer.WritePropertyName("CodeOffset");
        writer.WriteValue(CodeOffset);
        writer.WritePropertyName("Parameters");
        serializer.Serialize(writer, Parameters);
    }
}

public class EX_AutoRtfmAbortIfNot : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_AutoRtfmAbortIfNot;
}

[JsonConverter(typeof(FScriptTextConverter))]
public class FScriptText
{
    public EBlueprintTextLiteralType TextLiteralType;
    public KismetExpression? SourceString;
    public KismetExpression? KeyString;
    public KismetExpression? Namespace;
    public FPackageIndex? StringTableAsset;
    public KismetExpression? TableIdString;

    public FScriptText(FKismetArchive Ar)
    {
        TextLiteralType = (EBlueprintTextLiteralType)Ar.Read<byte>();
        switch (TextLiteralType)
        {
            case EBlueprintTextLiteralType.Empty:
                break;
            case EBlueprintTextLiteralType.LocalizedText:
                SourceString = Ar.ReadExpression();
                KeyString = Ar.ReadExpression();
                Namespace = Ar.ReadExpression();
                break;
            case EBlueprintTextLiteralType.InvariantText: // IsCultureInvariant
                SourceString = Ar.ReadExpression();
                break;
            case EBlueprintTextLiteralType.LiteralString:
                SourceString = Ar.ReadExpression();
                break;
            case EBlueprintTextLiteralType.StringTableEntry:
                StringTableAsset = new FPackageIndex(Ar);
                TableIdString = Ar.ReadExpression();
                KeyString = Ar.ReadExpression();
                break;
        }
    }
}
