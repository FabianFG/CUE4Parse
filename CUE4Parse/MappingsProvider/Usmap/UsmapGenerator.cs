using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using CUE4Parse.Utils;

namespace CUE4Parse.MappingsProvider.Usmap;

public static class UsmapGenerator
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ReportDiagnostic([MarshalAs(UnmanagedType.LPUTF8Str)] string message);

    [DllImport(CUE4ParseNatives.LibraryName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern unsafe bool nGenerateUsmapFromMemory(byte* bytes, nuint size, string output, byte compression, ReportDiagnostic report);

    public static bool Generate(string executable, string output, EUsmapCompressionMethod compression)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);
        ArgumentException.ThrowIfNullOrWhiteSpace(output);

        if (!OperatingSystem.IsWindows() || !CUE4ParseNatives.IsFeatureAvailable("UsmapGen\0"u8))
        {
            Log.Warning("Static mapping generation requires CUE4Parse-Natives built with UsmapGen");
            return false;
        }

        try
        {
            using var file = File.OpenRead(executable);
            var size = file.Length;
            if (size == 0)
            {
                Log.Warning("UsmapGen cannot read the empty executable {Executable}", executable);
                return false;
            }

            using var mapping = MemoryMappedFile.CreateFromFile(file, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, leaveOpen: true);
            using var view = mapping.CreateViewAccessor(0, size, MemoryMappedFileAccess.Read);
            unsafe
            {
                var handle = view.SafeMemoryMappedViewHandle;
                byte* bytes = null;
                handle.AcquirePointer(ref bytes);
                try
                {
                    return nGenerateUsmapFromMemory(bytes + view.PointerOffset, (nuint) size, output, (byte) compression, LogDiagnostic);
                }
                finally
                {
                    handle.ReleasePointer();
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or EntryPointNotFoundException or DllNotFoundException)
        {
            Log.Warning(ex, "UsmapGen could not process {Executable}", executable);
            return false;
        }
    }

    private static void LogDiagnostic(string message) => Log.Warning("UsmapGen: {Message}", message);
}
