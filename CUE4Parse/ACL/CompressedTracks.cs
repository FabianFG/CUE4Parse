using System;
using System.Runtime.InteropServices;
using static CUE4Parse.ACL.ACLException;
using static CUE4Parse.ACL.ACLNative;

namespace CUE4Parse.ACL
{
    public class CompressedTracks
    {
        internal IntPtr _handle;

        public CompressedTracks(byte[] buffer)
        {
            var mem = nAlignedMalloc(buffer.Length, 16);
            Marshal.Copy(buffer, 0, mem, buffer.Length);
            var outErrorResult = PrepareErrorResult();
            _handle = nMakeCompressedTracks(mem, outErrorResult);
            //Marshal.FreeHGlobal(mem); TODO IDK when to free
            CheckError(outErrorResult);
        }

        public string? IsValid(bool checkHash) => Marshal.PtrToStringAnsi(nCompressedTracks_IsValid(_handle, checkHash));

        [DllImport(LIB_NAME)]
        private static extern IntPtr nMakeCompressedTracks(IntPtr buffer, IntPtr outErrorResult);

        [DllImport(LIB_NAME)]
        private static extern IntPtr nCompressedTracks_IsValid(IntPtr handle, bool checkHash);
    }
}