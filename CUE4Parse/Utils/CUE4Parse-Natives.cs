using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace CUE4Parse.Utils; 

public static unsafe class CUE4ParseNatives 
{
    public const string LibraryName = "CUE4Parse-Natives";

    public static nint LibraryHandle { get; }
    public static bool IsInitialized => LibraryHandle != nint.Zero;

    private static readonly delegate* unmanaged<byte*, bool> _isFeatureAvailableFunctionPointer;

    static CUE4ParseNatives()
    {
        if (!NativeLibrary.TryLoad(
            LibraryName,
            Assembly.GetExecutingAssembly(),
            DllImportSearchPath.AssemblyDirectory,
            out var handle))
        {
            LibraryHandle = nint.Zero;
            return;
        }

        if (!NativeLibrary.TryGetExport(handle, "IsFeatureAvailable", out var isFeatureAvailableAddress))
        {
            NativeLibrary.Free(handle);
            LibraryHandle = nint.Zero;
            return;
        }

        _isFeatureAvailableFunctionPointer = (delegate* unmanaged<byte*, bool>)isFeatureAvailableAddress;
        LibraryHandle = handle;
    }

    public static bool IsFeatureAvailable(ReadOnlySpan<byte> utf8FeatureName)
    {
        if (!IsInitialized || utf8FeatureName.Length < 1 || utf8FeatureName[^1] != 0)
            return false;

        fixed (byte* featureNamePtr = utf8FeatureName)
        {
            return _isFeatureAvailableFunctionPointer(featureNamePtr);
        }
    }

    public static bool IsFeatureAvailable(ReadOnlySpan<char> featureName)
    {
        if (!IsInitialized || featureName.IsEmpty || featureName.Length > 128)
            return false;

        var utf8Chars = Encoding.UTF8.GetByteCount(featureName) + 1;
        Span<byte> utf8FeatureName = stackalloc byte[utf8Chars];
        if (!Encoding.UTF8.TryGetBytes(featureName, utf8FeatureName, out _))
            return false;

        return IsFeatureAvailable(utf8FeatureName);
    }
}
