using System.Runtime.InteropServices;

namespace CUE4Parse_Conversion.UEFormat.Natives;

internal sealed class NativePinScope : IDisposable
{
    private readonly List<GCHandle> _handles = [];
    private readonly List<IntPtr> _strings = [];
    private bool _disposed;

    public IntPtr PinArray<T>(T[]? array) where T : unmanaged
    {
        if (array is null || array.Length == 0)
            return IntPtr.Zero;

        var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
        _handles.Add(handle);
        return handle.AddrOfPinnedObject();
    }

    public IntPtr AllocUtf8(string? value)
    {
        var ptr = Marshal.StringToCoTaskMemUTF8(value ?? string.Empty);
        _strings.Add(ptr);
        return ptr;
    }

    public IntPtr PinStruct<T>(ref T value) where T : unmanaged
    {
        var array = new T[] { value };
        return PinArray(array);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var handle in _handles)
        {
            if (handle.IsAllocated)
                handle.Free();
        }

        foreach (var ptr in _strings)
            Marshal.FreeCoTaskMem(ptr);

        _handles.Clear();
        _strings.Clear();
    }
}
