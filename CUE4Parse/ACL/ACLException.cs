using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CUE4Parse.ACL
{
    public class ACLException : Exception
    {
        public ACLException(string? message) : base(message) { }

        // struct error_result { const char* error; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr PrepareErrorResult()
        {
            var outErrorResult = Marshal.AllocHGlobal(IntPtr.Size);
            Marshal.WriteIntPtr(outErrorResult, IntPtr.Zero);
            return outErrorResult;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckError(IntPtr errorResult)
        {
            var error = Marshal.PtrToStringAnsi(Marshal.ReadIntPtr(errorResult));
            Marshal.FreeHGlobal(errorResult);
            if (error != null)
            {
                throw new ACLException(error);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckError(string? error)
        {
            if (error is { Length: > 0 })
            {
                throw new ACLException(error);
            }
        }
    }
}