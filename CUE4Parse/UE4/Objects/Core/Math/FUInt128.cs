using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    public class FUInt128 : IUStruct
    {
        /** Internal values representing this number */
        public ulong Hi;
        public ulong Lo;

        public FUInt128() : this(0, 0) { }
        public FUInt128(ulong a)
        {
            Hi = 0;
            Lo = a;
        }

        public FUInt128(ulong a, ulong b)
        {
            Hi = a;
            Lo = b;
        }

        public FUInt128(uint A, uint B, uint C, uint D)
        {
            Hi = ((ulong)A << 32) | B;
            Lo = ((ulong)C << 32) | D;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetQuadPart(uint part, uint value)
        {
            switch (part)
            {
                case 3: Hi &= 4294967295u | ((ulong)value << 32); break;
                case 2: Hi &= 18446744069414584320u | value; break;
                case 1: Lo &= 4294967295u | ((ulong)value << 32); break;
                case 0: Lo &= 18446744069414584320u | value; break;
                default: break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetQuadPart(uint part)
        {
            switch (part)
            {
                case 3: return (uint)(Hi >> 32);
                case 2: return (uint)Hi;
                case 1: return (uint)(Lo >> 32);
                case 0: return (uint)Lo;
                default: break;
            }
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint DivideInternal(uint dividend, uint divisor, ref uint remainder)
        {
            ulong value = ((ulong)remainder << 32) | dividend;
            remainder = (uint)(value % divisor);
            return (uint)(value / divisor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsGreater(FUInt128 other)
        {
            if (Hi == other.Hi)
            {
                return Lo > other.Lo;
            }
            return Hi > other.Hi;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsLess(FUInt128 other)
        {
            if (Hi == other.Hi)
            {
                return Lo < other.Lo;
            }
            return Hi < other.Hi;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FUInt128 Divide(uint divisor, out uint remainder)
        {
            remainder = 0;

            SetQuadPart(3, DivideInternal(GetQuadPart(3), divisor, ref remainder));
            SetQuadPart(2, DivideInternal(GetQuadPart(2), divisor, ref remainder));
            SetQuadPart(1, DivideInternal(GetQuadPart(1), divisor, ref remainder));
            SetQuadPart(0, DivideInternal(GetQuadPart(0), divisor, ref remainder));
            return this;
        }

        public override string ToString() => $"Hi={Hi} Lo={Lo}";
    }
}
