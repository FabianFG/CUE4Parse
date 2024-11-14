using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Material;

public class FMaterialResourceProxyReader : FArchive
{
    protected readonly FArchive InnerArchive;
    public FNameEntrySerialized[]? NameMap;
    public FMaterialResourceLocOnDisk[]? Locs;
    private long _offsetToFirstResource;
    public readonly bool isGlobal;

    public FMaterialResourceProxyReader(FArchive Ar, bool bIsGlobal = false) : base(Ar.Versions)
    {
        InnerArchive = Ar;
        isGlobal = bIsGlobal;
        if (!isGlobal)
        {
            NameMap = InnerArchive.ReadArray(() => new FNameEntrySerialized(Ar));
            Locs = InnerArchive.ReadArray<FMaterialResourceLocOnDisk>();
            var _ = InnerArchive.Read<int>(); // NumBytes
        }
        _offsetToFirstResource = InnerArchive.Position;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct FMaterialResourceLocOnDisk
    {
        /** Relative offset to package (uasset/umap + uexp) beginning */
        public readonly uint Offset;
        public readonly ERHIFeatureLevel FeatureLevel;
        public readonly EMaterialQualityLevel QualityLevel;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override FName ReadFName()
    {
        var nameIndex = InnerArchive.Read<int>();
        var number = InnerArchive.Read<int>();
#if !NO_FNAME_VALIDATION
        if (nameIndex < 0 || nameIndex >= NameMap.Length)
        {
            throw new ParserException(InnerArchive, $"FName could not be read, requested index {nameIndex}, name map size {NameMap.Length}");
        }
#endif
        return new FName(NameMap[nameIndex], nameIndex, number);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int Read(byte[] buffer, int offset, int count)
        => InnerArchive.Read(buffer, offset, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override long Seek(long offset, SeekOrigin origin)
        => InnerArchive.Seek(offset, origin);

    public override bool CanSeek => InnerArchive.CanSeek;
    public override long Length => InnerArchive.Length;

    public override long Position
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => InnerArchive.Position;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => InnerArchive.Position = value;
    }

    public override string Name => InnerArchive.Name;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override T Read<T>()
        => InnerArchive.Read<T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override byte[] ReadBytes(int length)
        => InnerArchive.ReadBytes(length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override unsafe void Serialize(byte* ptr, int length)
        => InnerArchive.Serialize(ptr, length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override T[] ReadArray<T>(int length)
        => InnerArchive.ReadArray<T>(length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void ReadArray<T>(T[] array)
        => InnerArchive.ReadArray(array);

    public override object Clone() => new FMaterialResourceProxyReader((FArchive) InnerArchive.Clone());
}

public enum EMaterialQualityLevel : byte
{
    Low,
    High,
    Medium,
    Epic,
    Num
}

public enum ERHIFeatureLevel : byte
{
    /** Feature level defined by the core capabilities of OpenGL ES2. Deprecated */
    ES2_REMOVED,

    /** Feature level defined by the core capabilities of OpenGL ES3.1 & Metal/Vulkan. */
    ES3_1,

    /**
         * Feature level defined by the capabilities of DX10 Shader Model 4.
         * SUPPORT FOR THIS FEATURE LEVEL HAS BEEN ENTIRELY REMOVED.
         */
    SM4_REMOVED,

    /**
         * Feature level defined by the capabilities of DX11 Shader Model 5.
         *   Compute shaders with shared memory, group sync, UAV writes, integer atomics
         *   Indirect drawing
         *   Pixel shaders with UAV writes
         *   Cubemap arrays
         *   Read-only depth or stencil views (eg read depth buffer as SRV while depth test and stencil write)
         * Tessellation is not considered part of Feature Level SM5 and has a separate capability flag.
         */
    SM5,

    /**
         * Feature level defined by the capabilities of DirectX 12 hardware feature level 12_2 with Shader Model 6.5
         *   Raytracing Tier 1.1
         *   Mesh and Amplification shaders
         *   Variable rate shading
         *   Sampler feedback
         *   Resource binding tier 3
         */
    SM6,
    Num
}
