using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CUE4Parse.Utils;
using CUE4Parse_Conversion.Dto;

namespace CUE4Parse_Conversion.Writers.Alembic;

public static partial class AlembicGroom
{
    [LibraryImport(CUE4ParseNatives.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint nCreateAlembicGroom(string name);

    [LibraryImport(CUE4ParseNatives.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int nWriteAlembicGroomCurves(nint archive, int groupId, [MarshalAs(UnmanagedType.I1)] bool isGuide, ReadOnlySpan<int> sizes, int curveCount, ReadOnlySpan<byte> points, int pointCount, ReadOnlySpan<byte> rootUvs);

    // nint here instead of byte* so I don't have to use unsafe context
    [LibraryImport(CUE4ParseNatives.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint nFinishAlembicGroom(nint archive, out nuint length);

    [LibraryImport(CUE4ParseNatives.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void nDestroyAlembicGroom(nint archive);

    public static byte[] Write(GroomDto groom, string name, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!CUE4ParseNatives.IsFeatureAvailable("Alembic\0"u8) || !NativeLibrary.TryGetExport(CUE4ParseNatives.LibraryHandle, nameof(nWriteAlembicGroomCurves), out _))
            throw new NotSupportedException("Alembic export requires CUE4Parse-Natives built with Alembic support.");

        var Ar = nCreateAlembicGroom(name);
        if (Ar == nint.Zero)
            throw new IOException("Alembic could not create the groom archive.");

        try
        {
            foreach (var group in groom.Groups)
            {
                if (group.Strands is { } strands)
                {
                    WriteCurves(Ar, group.Index, false, strands, ct);
                }

                if (group.Guides is { } guides)
                {
                    WriteCurves(Ar, group.Index, true, guides, ct);
                }
            }

            ct.ThrowIfCancellationRequested();
            var bytes = nFinishAlembicGroom(Ar, out var length);
            if (bytes == nint.Zero || length == 0)
                throw new IOException("Alembic could not finish the groom archive.");
            if (length > (nuint) Array.MaxLength)
                throw new IOException("The Alembic archive is too large to export.");

            var output = GC.AllocateUninitializedArray<byte>((int) length);
            Marshal.Copy(bytes, output, 0, output.Length);
            ct.ThrowIfCancellationRequested();
            return output;
        }
        finally
        {
            nDestroyAlembicGroom(Ar);
        }
    }

    private static void WriteCurves(nint archive, int groupId, bool isGuide, GroomCurvesDto curves, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var points = MemoryMarshal.AsBytes(curves.Points.AsSpan());
        var rootUvs = MemoryMarshal.AsBytes(curves.RootUvs.AsSpan());
        if (nWriteAlembicGroomCurves(archive, groupId, isGuide, curves.Sizes, curves.Sizes.Length, points, curves.Points.Length, rootUvs) == 0)
            throw new IOException($"Alembic could not write the curves in groom group {groupId}.");
    }
}
