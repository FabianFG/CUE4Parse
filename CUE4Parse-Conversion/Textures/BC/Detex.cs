using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CUE4Parse_Conversion.Textures.BC;


[StructLayout(LayoutKind.Sequential)]
internal unsafe struct detexTexture
{
    public uint format;
    public byte* data;
    public int width;
    public int height;
    public int width_in_blocks;
    public int height_in_blocks;
}

/// <summary>
/// Wrapper class for the native zlib-ng library
/// </summary>
public unsafe class Detex : IDisposable
{
    /// <summary>
    /// Library handle for the current Detex instance
    /// </summary>
    private nint Handle { get; }

    private delegate* unmanaged<detexTexture*, byte*, uint, bool> DetexDecompressTextureLinear { get; }

    /// <summary>
    /// Initializes via a native Detex library path
    /// </summary>
    /// <returns/>
    /// <param name="libraryPath">The path of the native Detex library to be loaded</param>
    /// <inheritdoc cref="NativeLibrary.Load(string)"/>
    public Detex(string libraryPath) : this(NativeLibrary.Load(libraryPath)) { }

    private Detex(nint handle)
    {
        //Util.ThrowIfNull(handle);
        Handle = handle;

        var decompressTextureAddress = NativeLibrary.GetExport(Handle, "detexDecompressTextureLinear");
        DetexDecompressTextureLinear = (delegate* unmanaged<detexTexture*, byte*, uint, bool>)decompressTextureAddress;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool DecodeDetexLinear(byte[] inp, byte[] dst, int width, int height, DetexTextureFormat inputFormat, DetexPixelFormat outputPixelFormat)
    {
        fixed (byte* inpPtr = inp, dstPtr = dst)
        {
            detexTexture tex;
            tex.format = (uint)inputFormat;
            tex.data = inpPtr;
            tex.width = width;
            tex.height = height;
            tex.width_in_blocks = width / 4;
            tex.height_in_blocks = height / 4;

            return DetexDecompressTextureLinear(&tex, dstPtr, (uint)outputPixelFormat);
        }
    }

    private void ReleaseUnmanagedResources()
    {
        if (Handle != nint.Zero)
            NativeLibrary.Free(Handle);
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~Detex()
    {
        ReleaseUnmanagedResources();
    }
}
