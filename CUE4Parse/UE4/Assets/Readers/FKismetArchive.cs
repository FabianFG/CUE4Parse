using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Kismet;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Readers;

public class FKismetArchive : FArchive
{
    private readonly byte[] _data;
    public readonly IPackage Owner;
    public int Index;

    public FKismetArchive(string name, byte[] data, IPackage owner, VersionContainer? versions = null) : base(versions)
    {
        _data = data;
        Name = name;
        Owner = owner;
        Length = _data.Length;
    }

    public KismetExpression ReadExpression()
    {
        var index = Index;
        EExprToken token = (EExprToken)Read<byte>();
        KismetExpression expression = token switch
        {
            EExprToken.EX_LocalVariable => new EX_LocalVariable(this),
            EExprToken.EX_InstanceVariable => new EX_InstanceVariable(this),
            EExprToken.EX_DefaultVariable => new EX_DefaultVariable(this),
            EExprToken.EX_Return => new EX_Return(this),
            EExprToken.EX_Jump => new EX_Jump(this),
            EExprToken.EX_JumpIfNot => new EX_JumpIfNot(this),
            EExprToken.EX_Assert => new EX_Assert(this),
            EExprToken.EX_Nothing => new EX_Nothing(),
            EExprToken.EX_NothingInt32 => new EX_NothingInt32(this),
            EExprToken.EX_Let => new EX_Let(this),
            EExprToken.EX_ClassContext => new EX_ClassContext(this),
            EExprToken.EX_BitFieldConst => new EX_BitFieldConst(this),
            EExprToken.EX_MetaCast => new EX_MetaCast(this),
            EExprToken.EX_LetBool => new EX_LetBool(this),
            EExprToken.EX_EndParmValue => new EX_EndParmValue(),
            EExprToken.EX_EndFunctionParms => new EX_EndFunctionParms(),
            EExprToken.EX_Self => new EX_Self(),
            EExprToken.EX_Skip => new EX_Skip(this),
            EExprToken.EX_Context => new EX_Context(this),
            EExprToken.EX_Context_FailSilent => new EX_Context_FailSilent(this),
            EExprToken.EX_VirtualFunction => new EX_VirtualFunction(this),
            EExprToken.EX_FinalFunction => new EX_FinalFunction(this),
            EExprToken.EX_IntConst => new EX_IntConst(this),
            EExprToken.EX_FloatConst => new EX_FloatConst(this),
            EExprToken.EX_StringConst => new EX_StringConst(this),
            EExprToken.EX_ObjectConst => new EX_ObjectConst(this),
            EExprToken.EX_NameConst => new EX_NameConst(this),
            EExprToken.EX_RotationConst => new EX_RotationConst(this),
            EExprToken.EX_VectorConst => new EX_VectorConst(this),
            EExprToken.EX_ByteConst => new EX_ByteConst(this),
            EExprToken.EX_IntZero => new EX_IntZero(),
            EExprToken.EX_IntOne => new EX_IntOne(),
            EExprToken.EX_True => new EX_True(),
            EExprToken.EX_False => new EX_False(),
            EExprToken.EX_TextConst => new EX_TextConst(this),
            EExprToken.EX_NoObject => new EX_NoObject(),
            EExprToken.EX_TransformConst => new EX_TransformConst(this),
            EExprToken.EX_IntConstByte => new EX_IntConstByte(this),
            EExprToken.EX_NoInterface => new EX_NoInterface(),
            EExprToken.EX_DynamicCast => new EX_DynamicCast(this),
            EExprToken.EX_StructConst => new EX_StructConst(this),
            EExprToken.EX_EndStructConst => new EX_EndStructConst(),
            EExprToken.EX_SetArray => new EX_SetArray(this),
            EExprToken.EX_EndArray => new EX_EndArray(),
            EExprToken.EX_PropertyConst => new EX_PropertyConst(this),
            EExprToken.EX_UnicodeStringConst => new EX_UnicodeStringConst(this),
            EExprToken.EX_Int64Const => new EX_Int64Const(this),
            EExprToken.EX_UInt64Const => new EX_UInt64Const(this),
            EExprToken.EX_DoubleConst => new EX_DoubleConst(this),
            EExprToken.EX_Cast => new EX_Cast(this),
            EExprToken.EX_SetSet => new EX_SetSet(this),
            EExprToken.EX_EndSet => new EX_EndSet(),
            EExprToken.EX_SetMap => new EX_SetMap(this),
            EExprToken.EX_EndMap => new EX_EndMap(),
            EExprToken.EX_SetConst => new EX_SetConst(this),
            EExprToken.EX_EndSetConst => new EX_EndSetConst(),
            EExprToken.EX_MapConst => new EX_MapConst(this),
            EExprToken.EX_EndMapConst => new EX_EndMapConst(),
            EExprToken.EX_Vector3fConst => new EX_Vector3fConst(this),
            EExprToken.EX_StructMemberContext => new EX_StructMemberContext(this),
            EExprToken.EX_LetMulticastDelegate => new EX_LetMulticastDelegate(this),
            EExprToken.EX_LetDelegate => new EX_LetDelegate(this),
            EExprToken.EX_LocalVirtualFunction => new EX_LocalVirtualFunction(this),
            EExprToken.EX_LocalFinalFunction => new EX_LocalFinalFunction(this),
            EExprToken.EX_LocalOutVariable => new EX_LocalOutVariable(this),
            EExprToken.EX_DeprecatedOp4A => new EX_DeprecatedOp4A(),
            EExprToken.EX_InstanceDelegate => new EX_InstanceDelegate(this),
            EExprToken.EX_PushExecutionFlow => new EX_PushExecutionFlow(this),
            EExprToken.EX_PopExecutionFlow => new EX_PopExecutionFlow(),
            EExprToken.EX_ComputedJump => new EX_ComputedJump(this),
            EExprToken.EX_PopExecutionFlowIfNot => new EX_PopExecutionFlowIfNot(this),
            EExprToken.EX_Breakpoint => new EX_Breakpoint(),
            EExprToken.EX_InterfaceContext => new EX_InterfaceContext(this),
            EExprToken.EX_ObjToInterfaceCast => new EX_ObjToInterfaceCast(this),
            EExprToken.EX_EndOfScript => new EX_EndOfScript(),
            EExprToken.EX_CrossInterfaceCast => new EX_CrossInterfaceCast(this),
            EExprToken.EX_InterfaceToObjCast => new EX_InterfaceToObjCast(this),
            EExprToken.EX_WireTracepoint => new EX_WireTracepoint(),
            EExprToken.EX_SkipOffsetConst => new EX_SkipOffsetConst(this),
            EExprToken.EX_AddMulticastDelegate => new EX_AddMulticastDelegate(this),
            EExprToken.EX_ClearMulticastDelegate => new EX_ClearMulticastDelegate(this),
            EExprToken.EX_Tracepoint => new EX_Tracepoint(),
            EExprToken.EX_LetObj => new EX_LetObj(this),
            EExprToken.EX_LetWeakObjPtr => new EX_LetWeakObjPtr(this),
            EExprToken.EX_BindDelegate => new EX_BindDelegate(this),
            EExprToken.EX_RemoveMulticastDelegate => new EX_RemoveMulticastDelegate(this),
            EExprToken.EX_CallMulticastDelegate => new EX_CallMulticastDelegate(this),
            EExprToken.EX_LetValueOnPersistentFrame => new EX_LetValueOnPersistentFrame(this),
            EExprToken.EX_ArrayConst => new EX_ArrayConst(this),
            EExprToken.EX_EndArrayConst => new EX_EndArrayConst(),
            EExprToken.EX_SoftObjectConst => new EX_SoftObjectConst(this),
            EExprToken.EX_CallMath => new EX_CallMath(this),
            EExprToken.EX_SwitchValue => new EX_SwitchValue(this),
            EExprToken.EX_InstrumentationEvent => new EX_InstrumentationEvent(this),
            EExprToken.EX_ArrayGetByRef => new EX_ArrayGetByRef(this),
            EExprToken.EX_ClassSparseDataVariable => new EX_ClassSparseDataVariable(this),
            EExprToken.EX_FieldPathConst => new EX_FieldPathConst(this),
            EExprToken.EX_AutoRtfmStopTransact => new EX_AutoRtfmStopTransact(this),
            EExprToken.EX_AutoRtfmTransact => new EX_AutoRtfmTransact(this),
            EExprToken.EX_AutoRtfmAbortIfNot => new EX_AutoRtfmAbortIfNot(),
            _ => throw new ParserException($"Unknown EExprToken {token}")
        };
        expression.StatementIndex = index;
        return expression;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string XFERSTRING()
    {
        var eos = Array.IndexOf<byte>(_data, 0, (int)Position);
        if (eos == -1) throw new ParserException("Couldn't find end of the string");
        return Encoding.ASCII.GetString(ReadBytes(eos-(int)Position));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string XFERUNICODESTRING()
    {
        var pos = (int)Position;
        var length = -1;
        Span<byte> terminator = stackalloc byte[2];
        do
        {
            length += _data.AsSpan(pos + length + 1).IndexOf(terminator) + 1;
        }
        while (length % 2 != 0 || length == -1);
        if (length == -1) throw new ParserException("Couldn't find end of the unicode string");
        return Encoding.Unicode.GetString(ReadBytes(length));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public KismetExpression[] ReadExpressionArray(EExprToken endToken)
    {
        var newData = new List<KismetExpression>();
        KismetExpression currExpression = null;
        while (currExpression == null || currExpression.Token != endToken)
        {
            if (currExpression != null) newData.Add(currExpression);
            currExpression = ReadExpression();
        }

        return newData.ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override FName ReadFName()
    {
        var nameIndex = Read<int>();
        var extraIndex = Read<int>();
        Index += 4;
#if !NO_FNAME_VALIDATION
        if (nameIndex < 0 || nameIndex >= Owner.NameMap.Length)
        {
            throw new ParserException(this, $"FName could not be read, requested index {nameIndex}, name map size {Owner.NameMap.Length}");
        }
#endif
        return new FName(Owner.NameMap[nameIndex], nameIndex, extraIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int Read(byte[] buffer, int offset, int count)
    {
        int n = (int) (Length - Position);
        if (n > count) n = count;
        if (n <= 0)
            return 0;

        if (n <= 8)
        {
            int byteCount = n;
            while (--byteCount >= 0)
                buffer[offset + byteCount] = _data[Position + byteCount];
        }
        else
            Buffer.BlockCopy(_data, (int) Position, buffer, offset, n);
        Position += n;

        return n;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override byte[] ReadBytes(int length)
    {
        var result = new byte[length];
        Read(result, 0, length);
        Index += length;
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override long Seek(long offset, SeekOrigin origin)
    {
        Position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException()
        };
        return Position;
    }

    public override bool CanSeek { get; } = true;
    public override long Length { get; }
    public override long Position { get; set; }
    public override string Name { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override T Read<T>()
    {
        var size = Unsafe.SizeOf<T>();
        var result = Unsafe.ReadUnaligned<T>(ref _data[Position]);
        Position += size;
        Index += size;
        return result;
    }

    public override object Clone() => new FKismetArchive(Name, _data, Owner, Versions) {Position = Position};
}
