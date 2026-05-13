using System;
using System.Runtime.InteropServices;
using Serilog;

namespace CUE4Parse.UE4.Lua.unluac;

public class Unluac : IDisposable
{
    private nint Handle { get; set; }
    private readonly unluac_create_isolate _createIsolate;
    private readonly unluac_tear_down_isolate _tearDownIsolate;
    private readonly unluac_decompile_buffer _decompileBuffer;
    private readonly unluac_free _free;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr unluac_create_isolate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int unluac_tear_down_isolate(IntPtr thread);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int unluac_decompile_buffer(
        IntPtr thread, uint flags,
        byte[] data, int len,
        byte[] opmap, int opmaplen,
        out IntPtr outPtr, out int outLen,
        out IntPtr logPtr, out int logLen);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void unluac_free(IntPtr thread, IntPtr ptr);

    public Unluac(string libraryPath) : this(NativeLibrary.Load(libraryPath)) { }

    public Unluac(nint handle)
    {
        ArgumentNullException.ThrowIfNull(handle);
        Handle = handle;

        _createIsolate = Marshal.GetDelegateForFunctionPointer<unluac_create_isolate>(NativeLibrary.GetExport(Handle, nameof(unluac_create_isolate)));
        _tearDownIsolate = Marshal.GetDelegateForFunctionPointer<unluac_tear_down_isolate>(NativeLibrary.GetExport(Handle, nameof(unluac_tear_down_isolate)));
        _decompileBuffer = Marshal.GetDelegateForFunctionPointer<unluac_decompile_buffer>(NativeLibrary.GetExport(Handle, nameof(unluac_decompile_buffer)));
        _free = Marshal.GetDelegateForFunctionPointer<unluac_free>(NativeLibrary.GetExport(Handle, nameof(unluac_free)));
    }

    public EUnluacErrorCode Decompile(byte[] luaBuffer, byte[] opmap, uint flags, out byte[] output, out byte[] log)
    {
        output = [];
        log = [];

        EUnluacErrorCode rc = EUnluacErrorCode.Ok;
        IntPtr thread = _createIsolate();
        if (thread == IntPtr.Zero)
        {
            Log.Error("Failed to create isolated thread");
            return rc;
        }

        IntPtr outPtr = IntPtr.Zero, logPtr = IntPtr.Zero;
        try
        {
            rc = (EUnluacErrorCode)_decompileBuffer(thread, flags, luaBuffer, luaBuffer.Length, opmap, opmap.Length,
                out outPtr, out int outLen, out logPtr, out int logLen);

            if (outPtr != IntPtr.Zero && outLen > 0)
            {
                output = new byte[outLen];
                Marshal.Copy(outPtr, output, 0, outLen);
            }

            if (logPtr != IntPtr.Zero && logLen > 0)
            {
                log = new byte[logLen];
                Marshal.Copy(logPtr, log, 0, logLen);
            }
        }
        catch (Exception e)
        {
            if (rc == EUnluacErrorCode.Ok) rc = EUnluacErrorCode.Error;
            Log.Error(e, "Failed to decompile lua buffer.");
        }
        finally
        {
            _free(thread, outPtr);
            _free(thread, logPtr);
            _tearDownIsolate(thread);
        }

        return rc;
    }

    private void ReleaseUnmanagedResources()
    {
        NativeLibrary.Free(Handle);
        Handle = nint.Zero;
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~Unluac()
    {
        ReleaseUnmanagedResources();
    }
}
