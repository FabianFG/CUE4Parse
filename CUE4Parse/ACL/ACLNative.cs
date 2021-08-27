using System;
using System.Runtime.InteropServices;

namespace CUE4Parse.ACL
{
    public static class ACLNative
    {
        public const string LIB_NAME = "CUE4Parse-Natives";

        [DllImport(LIB_NAME)]
        public static extern IntPtr nAllocate(int size, int alignment = 16);

        [DllImport(LIB_NAME)]
        public static extern void nDeallocate(IntPtr ptr, int size);

        // pure c# way:
        //var rawPtr = Marshal.AllocHGlobal(size + 8);
        //var aligned = new IntPtr(16 * (((long) rawPtr + 15) / 16));
    }
}