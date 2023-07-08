using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Versions;
using System;
using System.Text;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;

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

public class FKismetPropertyPointerConverter : JsonConverter<FKismetPropertyPointer>
{
    public override FKismetPropertyPointer ReadJson(JsonReader reader, Type objectType, FKismetPropertyPointer? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer, FKismetPropertyPointer? value, JsonSerializer serializer)
    {
        if (value == null) return;

        if (value.bNew)
        {
            value.New!.WriteJson(writer, serializer);
        }
        else
        {
            value.Old!.WriteJson(writer, serializer);
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

public class KismetExpressionConverter : JsonConverter<KismetExpression>
{
    public override void WriteJson(JsonWriter writer, KismetExpression? value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        value?.WriteJson(writer, serializer);
        writer.WriteEndObject();
    }

    public override KismetExpression ReadJson(JsonReader reader, Type objectType, KismetExpression? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public abstract class KismetExpression<T> : KismetExpression
{
    public T? Value;

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("Value");
        serializer.Serialize(writer, Value);
    }
}

public class EX_AddMulticastDelegate(FKismetArchive Ar) : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_AddMulticastDelegate;
    public KismetExpression Delegate = Ar.ReadExpression();
    public KismetExpression DelegateToAdd = Ar.ReadExpression();

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

public class EX_ArrayGetByRef(FKismetArchive Ar) : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_ArrayGetByRef;
    public KismetExpression ArrayVariable = Ar.ReadExpression();
    public KismetExpression ArrayIndex = Ar.ReadExpression();

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("ArrayVariable");
        serializer.Serialize(writer, ArrayVariable);
        writer.WritePropertyName("ArrayIndex");
        serializer.Serialize(writer, ArrayIndex);
    }
}

public class EX_Assert(FKismetArchive Ar) : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_Assert;
    public ushort LineNumber = Ar.Read<ushort>();
    public bool DebugMode = Ar.ReadFlag();
    public KismetExpression AssertExpression = Ar.ReadExpression();

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

public class EX_BindDelegate(FKismetArchive Ar) : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_BindDelegate;
    public FName FunctionName = Ar.ReadFName();
    public KismetExpression Delegate = Ar.ReadExpression();
    public KismetExpression ObjectTerm = Ar.ReadExpression();

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

    public EX_ByteConst(FArchive Ar)
    {
        Value = Ar.Read<byte>();
    }
}

public class EX_CallMath(FKismetArchive Ar) : EX_FinalFunction(Ar)
{
    public override EExprToken Token => EExprToken.EX_CallMath;
}

public class EX_CallMulticastDelegate(FKismetArchive Ar) : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_CallMulticastDelegate;
    public FPackageIndex StackNode = new FPackageIndex(Ar);
    public KismetExpression Delegate = Ar.ReadExpression();
    public KismetExpression[] Parameters = Ar.ReadExpressionArray(EExprToken.EX_EndFunctionParms);

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

public class EX_Cast(FKismetArchive Ar) : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_Cast; // EX_PrimitiveCast
    public ECastToken ConversionType = (ECastToken) Ar.Read<byte>();
    public KismetExpression Target = Ar.ReadExpression();

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("ConversionType");
        writer.WriteValue(ConversionType.ToString());
        writer.WritePropertyName("Target");
        serializer.Serialize(writer, Target);
    }
}

public abstract class EX_CastBase(FKismetArchive Ar) : KismetExpression
{
    public FPackageIndex ClassPtr = new FPackageIndex(Ar);
    public KismetExpression Target = Ar.ReadExpression();

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("InterfaceClass");
        serializer.Serialize(writer, ClassPtr);
        writer.WritePropertyName("Target");
        serializer.Serialize(writer, Target);
    }
}

public class EX_ClassContext(FKismetArchive Ar) : EX_Context(Ar)
{
    public override EExprToken Token => EExprToken.EX_ClassContext;
}

public class EX_ClassSparseDataVariable(FKismetArchive Ar) : EX_VariableBase(Ar)
{
    public override EExprToken Token => EExprToken.EX_ClassSparseDataVariable;
}

public class EX_ClearMulticastDelegate(FKismetArchive Ar) : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_ClearMulticastDelegate;
    public KismetExpression DelegateToClear = Ar.ReadExpression();

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("DelegateToClear");
        serializer.Serialize(writer, DelegateToClear);
    }
}

public class EX_ComputedJump(FKismetArchive Ar) : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_ComputedJump;
    public KismetExpression CodeOffsetExpression = Ar.ReadExpression();

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("OffsetExpression");
        serializer.Serialize(writer, CodeOffsetExpression);
    }
}

public class EX_Context(FKismetArchive Ar) : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_Context;
    public KismetExpression ObjectExpression = Ar.ReadExpression();
    public uint Offset = Ar.Read<uint>();
    public FKismetPropertyPointer RValuePointer = new(Ar);
    public KismetExpression ContextExpression = Ar.ReadExpression();

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

public class EX_Context_FailSilent(FKismetArchive Ar) : EX_Context(Ar)
{
    public override EExprToken Token => EExprToken.EX_Context_FailSilent;
}

public class EX_CrossInterfaceCast(FKismetArchive Ar) : EX_CastBase(Ar)
{
    public override EExprToken Token => EExprToken.EX_CrossInterfaceCast;
}

public class EX_DefaultVariable(FKismetArchive Ar) : EX_VariableBase(Ar)
{
    public override EExprToken Token => EExprToken.EX_DefaultVariable;
}

public class EX_DeprecatedOp4A : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_DeprecatedOp4A;
}

public class EX_DoubleConst : KismetExpression<double>
{
    public override EExprToken Token => EExprToken.EX_DoubleConst;

    public EX_DoubleConst(FArchive Ar)
    {
        Value = Ar.Read<double>();
    }
}

public class EX_DynamicCast(FKismetArchive Ar) : EX_CastBase(Ar)
{
    public override EExprToken Token => EExprToken.EX_DynamicCast;

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("Class");
        serializer.Serialize(writer, ClassPtr);
        writer.WritePropertyName("Target");
        serializer.Serialize(writer, Target);
    }
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

public class EX_FinalFunction(FKismetArchive Ar) : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_FinalFunction;
    public FPackageIndex StackNode = new(Ar);
    public KismetExpression[] Parameters = Ar.ReadExpressionArray(EExprToken.EX_EndFunctionParms);

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("Function");
        serializer.Serialize(writer, StackNode);
        writer.WritePropertyName("Parameters");
        serializer.Serialize(writer, Parameters);

        if (Parameters is [EX_IntConst offsetint])
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

    public EX_FloatConst(FArchive Ar)
    {
        Value = Ar.Read<float>();
    }
}

public class EX_InstanceDelegate(FArchive Ar) : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_InstanceDelegate;
    public FName FunctionName = Ar.ReadFName();

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("FunctionName");
        serializer.Serialize(writer, FunctionName);
    }
}

public class EX_InstanceVariable(FKismetArchive Ar) : EX_VariableBase(Ar)
{
    public override EExprToken Token => EExprToken.EX_InstanceVariable;
}

public class EX_InstrumentationEvent : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_InstrumentationEvent;

    public EScriptInstrumentationType EventType;
    public FName? EventName;

    public EX_InstrumentationEvent(FArchive Ar)
    {
        EventType = (EScriptInstrumentationType) Ar.Read<byte>();

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

    public EX_Int64Const(FArchive Ar)
    {
        Value = Ar.Read<long>();
    }
}

public class EX_IntConst : KismetExpression<int>
{
    public override EExprToken Token => EExprToken.EX_IntConst;

    public EX_IntConst(FArchive Ar)
    {
        Value = Ar.Read<int>();
    }
}

public class EX_IntConstByte : KismetExpression<byte>
{
    public override EExprToken Token => EExprToken.EX_IntConstByte;

    public EX_IntConstByte(FArchive Ar)
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

public class EX_InterfaceContext(FKismetArchive Ar) : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_InterfaceContext;
    public KismetExpression InterfaceValue = Ar.ReadExpression();

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("InterfaceValue");
        serializer.Serialize(writer, InterfaceValue);
    }
}

public class EX_InterfaceToObjCast(FKismetArchive Ar) : EX_CastBase(Ar)
{
    public override EExprToken Token => EExprToken.EX_InterfaceToObjCast;

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("ObjectClass");
        serializer.Serialize(writer, ClassPtr);
        writer.WritePropertyName("Target");
        serializer.Serialize(writer, Target);
    }
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

public class EX_JumpIfNot(FKismetArchive Ar) : EX_Jump(Ar)
{
    public override EExprToken Token => EExprToken.EX_JumpIfNot;
    public KismetExpression BooleanExpression = Ar.ReadExpression();

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("BooleanExpression");
        serializer.Serialize(writer, BooleanExpression);
    }
}

public class EX_Let(FKismetArchive Ar) : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_Let;
    public FKismetPropertyPointer Property = new(Ar);
    public KismetExpression Variable = Ar.ReadExpression();
    public KismetExpression Assignment = Ar.ReadExpression();

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

public abstract class EX_LetBase(FKismetArchive Ar) : KismetExpression
{
    public KismetExpression Variable = Ar.ReadExpression();
    public KismetExpression Assignment = Ar.ReadExpression();

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("Variable");
        serializer.Serialize(writer, Variable);
        writer.WritePropertyName("Expression");
        serializer.Serialize(writer, Assignment);
    }
}

public class EX_LetBool(FKismetArchive Ar) : EX_LetBase(Ar)
{
    public override EExprToken Token => EExprToken.EX_LetBool;
}

public class EX_LetDelegate(FKismetArchive Ar) : EX_LetBase(Ar)
{
    public override EExprToken Token => EExprToken.EX_LetDelegate;
}

public class EX_LetMulticastDelegate(FKismetArchive Ar) : EX_LetBase(Ar)
{
    public override EExprToken Token => EExprToken.EX_LetMulticastDelegate;
}

public class EX_LetObj(FKismetArchive Ar) : EX_LetBase(Ar)
{
    public override EExprToken Token => EExprToken.EX_LetObj;
}

public class EX_LetValueOnPersistentFrame(FKismetArchive Ar) : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_LetValueOnPersistentFrame;
    public FKismetPropertyPointer DestinationProperty = new(Ar);
    public KismetExpression AssignmentExpression = Ar.ReadExpression();

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("DestinationProperty");
        serializer.Serialize(writer, DestinationProperty);
        writer.WritePropertyName("AssignmentExpression");
        serializer.Serialize(writer, AssignmentExpression);
    }
}

public class EX_LetWeakObjPtr(FKismetArchive Ar) : EX_LetBase(Ar)
{
    public override EExprToken Token => EExprToken.EX_LetWeakObjPtr;
}

public class EX_LocalFinalFunction(FKismetArchive Ar) : EX_FinalFunction(Ar)
{
    public override EExprToken Token => EExprToken.EX_LocalFinalFunction;
}

public class EX_LocalOutVariable(FKismetArchive Ar) : EX_VariableBase(Ar)
{
    public override EExprToken Token => EExprToken.EX_LocalOutVariable;
}

public class EX_LocalVariable(FKismetArchive Ar) : EX_VariableBase(Ar)
{
    public override EExprToken Token => EExprToken.EX_LocalVariable;
}

public class EX_LocalVirtualFunction(FKismetArchive Ar) : EX_VirtualFunction(Ar)
{
    public override EExprToken Token => EExprToken.EX_LocalVirtualFunction;
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

public class EX_MetaCast(FKismetArchive Ar) : EX_CastBase(Ar)
{
    public override EExprToken Token => EExprToken.EX_MetaCast;

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("Class");
        serializer.Serialize(writer, ClassPtr);
        writer.WritePropertyName("Target");
        serializer.Serialize(writer, Target);
    }
}

public class EX_NameConst : KismetExpression<FName>
{
    public override EExprToken Token => EExprToken.EX_NameConst;

    public EX_NameConst(FArchive Ar)
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

public class EX_ObjToInterfaceCast(FKismetArchive Ar) : EX_CastBase(Ar)
{
    public override EExprToken Token => EExprToken.EX_ObjToInterfaceCast;
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

public class EX_PopExecutionFlowIfNot(FKismetArchive Ar) : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_PopExecutionFlowIfNot;
    public KismetExpression BooleanExpression = Ar.ReadExpression();

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("BooleanExpression");
        serializer.Serialize(writer, BooleanExpression);
    }
}

public class EX_PropertyConst(FKismetArchive Ar) : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_PropertyConst;
    public FKismetPropertyPointer Property = new(Ar);

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

public class EX_RemoveMulticastDelegate(FKismetArchive Ar) : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_RemoveMulticastDelegate;
    public KismetExpression Delegate = Ar.ReadExpression();
    public KismetExpression DelegateToAdd = Ar.ReadExpression();

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("MulticastDelegate");
        serializer.Serialize(writer, Delegate);
        writer.WritePropertyName("Delegate");
        serializer.Serialize(writer, DelegateToAdd);
    }
}

public class EX_Return(FKismetArchive Ar) : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_Return;
    public KismetExpression ReturnExpression = Ar.ReadExpression();

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

    public EX_RotationConst(FArchive Ar)
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

    public EX_SkipOffsetConst(FArchive Ar)
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
    public override EExprToken Token => EExprToken.EX_StringConst;

    public EX_StringConst(FKismetArchive Ar)
    {
        Value = Ar.XFERSTRING();
        Ar.Position++;
        Ar.Index++;
    }
}

public class EX_StructConst(FKismetArchive Ar) : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_StructConst;
    public FPackageIndex Struct = new(Ar);
    public int StructSize = Ar.Read<int>();
    public KismetExpression[] Properties = Ar.ReadExpressionArray(EExprToken.EX_EndStructConst);

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("Struct");
        serializer.Serialize(writer, Struct);
        writer.WritePropertyName("Properties");
        serializer.Serialize(writer, Properties);
    }
}

public class EX_StructMemberContext(FKismetArchive Ar) : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_StructMemberContext;
    public FKismetPropertyPointer Property = new(Ar);
    public KismetExpression StructExpression = Ar.ReadExpression();

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("Property");
        serializer.Serialize(writer, Property);
        writer.WritePropertyName("StructExpression");
        serializer.Serialize(writer, StructExpression);
    }
}

public struct FKismetSwitchCase(FKismetArchive Ar)
{
    public KismetExpression CaseIndexValueTerm = Ar.ReadExpression();
    public uint NextOffset = Ar.Read<uint>();
    public KismetExpression CaseTerm = Ar.ReadExpression();
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

    public EX_TransformConst(FArchive Ar)
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

    public EX_UInt64Const(FArchive Ar)
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

public abstract class EX_VariableBase(FKismetArchive Ar) : KismetExpression
{
    public FKismetPropertyPointer Variable = new(Ar);

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

    public EX_Vector3fConst(FArchive Ar)
    {
        Value = Ar.Read<FVector>();
    }
}

public class EX_VectorConst : KismetExpression<FVector>
{
    public override EExprToken Token => EExprToken.EX_VectorConst;

    public EX_VectorConst(FArchive Ar)
    {
        Value = new FVector(Ar);
    }
}

public class EX_VirtualFunction(FKismetArchive Ar) : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_VirtualFunction;
    public FName VirtualFunctionName = Ar.ReadFName();
    public KismetExpression[] Parameters = Ar.ReadExpressionArray(EExprToken.EX_EndFunctionParms);

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
        TextLiteralType = (EBlueprintTextLiteralType) Ar.Read<byte>();
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

public class FScriptTextConverter : JsonConverter<FScriptText>
{
    public override void WriteJson(JsonWriter writer, FScriptText? value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        switch (value?.TextLiteralType)
        {
            case EBlueprintTextLiteralType.Empty:
                writer.WritePropertyName("SourceString");
                writer.WriteValue("");
                break;
            case EBlueprintTextLiteralType.LocalizedText:
                writer.WritePropertyName("SourceString");
                serializer.Serialize(writer, value.SourceString);
                writer.WritePropertyName("KeyString");
                serializer.Serialize(writer, value.KeyString);
                writer.WritePropertyName("Namespace");
                serializer.Serialize(writer, value.Namespace);
                break;
            case EBlueprintTextLiteralType.InvariantText:
            case EBlueprintTextLiteralType.LiteralString:
                writer.WritePropertyName("SourceString");
                serializer.Serialize(writer, value.SourceString);
                break;
            case EBlueprintTextLiteralType.StringTableEntry:
                writer.WritePropertyName("StringTableAsset");
                serializer.Serialize(writer, value.StringTableAsset);
                writer.WritePropertyName("TableIdString");
                serializer.Serialize(writer, value.TableIdString);
                writer.WritePropertyName("KeyString");
                serializer.Serialize(writer, value.KeyString);
                break;
        }

        writer.WriteEndObject();
    }

    public override FScriptText ReadJson(JsonReader reader, Type objectType, FScriptText? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
