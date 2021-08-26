using System;
using System.Runtime.InteropServices;
using static CUE4Parse.ACL.ACLNative;

namespace CUE4Parse.ACL
{
    public class CompressedTracks
    {
        internal IntPtr _handle;
        private readonly bool _isOwner;

        public CompressedTracks(byte[] buffer)
        {
            _isOwner = true;
            _handle = nAlignedMalloc(buffer.Length, 16);
            Marshal.Copy(buffer, 0, _handle, buffer.Length);
            var error = IsValid(false);
            if (error != null)
            {
                nAlignedFree(_handle);
                _handle = IntPtr.Zero;
                throw new ACLException(error);
            }
        }

        public CompressedTracks(IntPtr existing)
        {
            _isOwner = false;
            _handle = existing;
        }

        ~CompressedTracks()
        {
            if (_isOwner && _handle != IntPtr.Zero) Marshal.FreeHGlobal(_handle);
        }

        public string? IsValid(bool checkHash)
        {
            var error = Marshal.PtrToStringAnsi(nCompressedTracks_IsValid(_handle, checkHash))!;
            return error.Length > 0 ? error : null;
        }

        public TracksHeader GetTracksHeader() => Marshal.PtrToStructure<TracksHeader>(_handle + Marshal.SizeOf<RawBufferHeader>());

        [DllImport(LIB_NAME)]
        private static extern IntPtr nCompressedTracks_IsValid(IntPtr handle, bool checkHash);
    }
}