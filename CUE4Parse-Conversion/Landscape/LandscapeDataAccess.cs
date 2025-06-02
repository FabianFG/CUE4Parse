using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse_Conversion.Landscape;

internal class FLandscapeComponentDataInterface {
    // offset of this component's data into heightmap texture
    private readonly ULandscapeComponent Component;
    private readonly bool bWorkOnEditingLayer;
    private readonly int HeightmapStride;
    private readonly int HeightmapComponentOffsetX;
    private readonly int HeightmapComponentOffsetY;
    public readonly int HeightmapSubsectionOffset;
    private readonly int MipLevel = 0;

    private readonly FColor[] HeightMipData;
    private readonly unsafe FColor* XYOffsetMipData;

    private readonly int ComponentSizeVerts;
    private readonly int SubsectionSizeVerts;
    private readonly int ComponentNumSubsections;

    private readonly byte[]?[] _cache;
    private readonly object _dataLock = new();

    private bool _bEnsuredWeightmapTexCache;
    
    internal unsafe FLandscapeComponentDataInterface(ULandscapeComponent inComponent, int inMipLevel) 
    {
        Component = inComponent;
        HeightMipData = null;
        XYOffsetMipData = null;
        MipLevel = inMipLevel;

        _cache = new byte[inComponent.GetWeightmapLayerAllocations().Length][];

        UTexture2D heightMapTexture = Component.GetHeightmap()!;

        var format = heightMapTexture.Format;
        Debug.Assert(heightMapTexture.Format == EPixelFormat.PF_B8G8R8A8);

        if (PixelFormatUtils.PixelFormats.ElementAtOrDefault((int)format) is not { Supported: true } formatInfo ||
            formatInfo.BlockBytes == 0)
            throw new NotImplementedException($"The supplied pixel format {format} is not supported!");

        HeightmapStride = heightMapTexture.PlatformData.SizeX >> MipLevel;
        HeightmapComponentOffsetX =
            (int)((heightMapTexture.PlatformData.SizeX >> MipLevel) * Component.HeightmapScaleBias.Z);
        HeightmapComponentOffsetY =
            (int)((heightMapTexture.PlatformData.SizeY >> MipLevel) * Component.HeightmapScaleBias.W);
        HeightmapSubsectionOffset = (Component.SubsectionSizeQuads + 1) >> MipLevel;

        ComponentSizeVerts = (Component.ComponentSizeQuads + 1) >> MipLevel;
        SubsectionSizeVerts = (Component.SubsectionSizeQuads + 1) >> MipLevel;
        ComponentNumSubsections = Component.NumSubsections;
        
        if (MipLevel < heightMapTexture.PlatformData.Mips.Length) {
            Trace.Assert(heightMapTexture.Owner != null, "heightMapTexture.Owner != null");
            var platform = heightMapTexture.Owner!.Provider!.Versions.Platform;

            var mip = heightMapTexture.GetMip(MipLevel);

            if (mip == null)
                throw new InvalidOperationException($"failed to get mip {MipLevel} from height map texture");

            var bulkData = mip.BulkData?.Data;

            if (bulkData == null)
                throw new InvalidOperationException("height map bulk data is null");

            if (platform == ETexturePlatform.XboxAndPlaystation)
                bulkData = PlatformDeswizzlers.DeswizzleXBPS(bulkData, mip, formatInfo);
            else if (platform == ETexturePlatform.NintendoSwitch)
                bulkData = PlatformDeswizzlers.GetDeswizzledData(bulkData, mip, formatInfo);

            var ar = new FStreamArchive("HeightMap",
                new MemoryStream(bulkData ?? throw new InvalidOperationException("height map bulk data is null")));
            HeightMipData = ar.ReadArray<FColor>(bulkData.Length / sizeof(FColor));
            Debug.Assert(ar.Position == bulkData.Length);
            Debug.Assert(HeightMipData.Length == bulkData.Length / sizeof(FColor));

            // if (Component.XYOffsetmapTexture != null)
            // {
            // 	XYOffsetMipData = Component.XYOffsetmapTexture.GetMipData(MipLevel);
            // }
        }
        else {
            throw new Exception("MipLevel >= heightMapTexture.PlatformData.Mips.Length");
        }
    }

    private bool GetWeightMapIndex(FWeightmapLayerAllocationInfo allocationInfo, out int LayerIdx) 
    {
        LayerIdx = -1;
        FWeightmapLayerAllocationInfo[] componentWeightmapLayerAllocations =
            Component.GetWeightmapLayerAllocations();
        UTexture2D[] componentWeightmapTextures = Component.GetWeightmapTextures();

        for (int Idx = 0; Idx < componentWeightmapLayerAllocations.Length; Idx++) {
            if (componentWeightmapLayerAllocations[Idx].LayerInfo.Equals(allocationInfo.LayerInfo)) {
                LayerIdx = Idx;
                break;
            }
        }

        if (LayerIdx < 0) {
            return false;
        }

        if (componentWeightmapLayerAllocations[LayerIdx].WeightmapTextureIndex >= componentWeightmapTextures.Length) {
            return false;
        }

        if (componentWeightmapLayerAllocations[LayerIdx].WeightmapTextureChannel >= 4) {
            return false;
        }

        return true;
    }

    public bool EnsureWeightmapTextureDataCache() 
    {
        var allocationInfos = Component.GetWeightmapLayerAllocations();
        for (var index = 0; index < allocationInfos.Length; index++) {
            var allocationInfo = allocationInfos[index];
            if (!GetWeightMapIndex(allocationInfo, out var LayerIdx)) {
                throw new ArgumentOutOfRangeException(); // "LayerInfo not found" should not happen
            }

            if (!GetWeightmapTextureData(index, out _)) {
                return false;
            }
        }

        _bEnsuredWeightmapTexCache = true;
        return true;
    }

    private bool GetWeightmapTextureData(int /*ULandscapeLayerInfoObject*/ layerIdx, out byte[]? outData) 
    {
        if (_bEnsuredWeightmapTexCache) {
            // can read without lock
            outData = _cache[layerIdx];
            return true;
        }

        lock (_dataLock) {
            if (_cache[layerIdx] != null) {
                outData = _cache[layerIdx];
                return true;
            }
        }


        outData = [];
        var componentWeightmapLayerAllocations = Component.GetWeightmapLayerAllocations();
        var componentWeightmapTextures = Component.GetWeightmapTextures();

        int weightmapSize = ((Component.SubsectionSizeQuads + 1) * Component.NumSubsections) >> MipLevel;
        outData = new byte[weightmapSize * weightmapSize]; // not *4 because we only want one channel

        // BGRA
        var weightTexture =
            componentWeightmapTextures[componentWeightmapLayerAllocations[layerIdx].WeightmapTextureIndex];
        var format = weightTexture.Format;
        if (PixelFormatUtils.PixelFormats.ElementAtOrDefault((int)format) is not { Supported: true } formatInfo ||
            formatInfo.BlockBytes == 0)
            throw new NotImplementedException($"The supplied pixel format {format} is not supported!");

        var platform = weightTexture.Owner!.Provider!.Versions.Platform;

        var mip = weightTexture.GetMip(MipLevel);

        var bulkData = mip.BulkData.Data;
        if (platform == ETexturePlatform.XboxAndPlaystation)
            bulkData = PlatformDeswizzlers.DeswizzleXBPS(bulkData!, mip, formatInfo);
        else if (platform == ETexturePlatform.NintendoSwitch)
            bulkData = PlatformDeswizzlers.GetDeswizzledData(bulkData!, mip, formatInfo);

        // FColor is BGRA
        // Channel remapping
        int[] channelOffsets = [
            (int)Marshal.OffsetOf(typeof(FColor), "R"),
            (int)Marshal.OffsetOf<FColor>("G"), (int)Marshal.OffsetOf<FColor>("B"), (int)Marshal.OffsetOf<FColor>("A")
        ];

        var offset = channelOffsets[componentWeightmapLayerAllocations[layerIdx].WeightmapTextureChannel];

        // separate the channel
        for (int i = 0; i < weightmapSize * weightmapSize; i++) {
            outData[i] = bulkData[i * 4 + offset];
        }

        lock (_dataLock) {
            _cache[layerIdx] = outData;
        }

        return true;
    }

    FColor GetHeightData(int localX, int localY) {
#if true //LANDSCAPE_VALIDATE_DATA_ACCESS
        Debug.Assert(Component != null);
        Debug.Assert(HeightMipData != null);
        Debug.Assert(localX >= 0 && localY >= 0 && localX < ComponentSizeVerts && localY < ComponentSizeVerts);
#endif

        VertexXYToTexelXY(localX, localY, out var texelX, out var texelY);
        return HeightMipData[
            texelX + HeightmapComponentOffsetX + (texelY + HeightmapComponentOffsetY) * HeightmapStride];
    }

    internal byte GetLayerWeight(int localX, int localY,
        FWeightmapLayerAllocationInfo? /*ULandscapeLayerInfoObject*/ allocationInfo) {
        if (GetWeightMapIndex(allocationInfo, out var LayerIdx)) {
            return GetLayerWeight(localX, localY, LayerIdx);
        }

        return 0;
    }

    private byte GetLayerWeight(int localX, int localY, int /*ULandscapeLayerInfoObject*/ layerIdx) {
#if true //LANDSCAPE_VALIDATE_DATA_ACCESS
        Debug.Assert(Component != null);
        Debug.Assert(HeightMipData != null);
        Debug.Assert(localX >= 0 && localY >= 0 && localX < ComponentSizeVerts && localY < ComponentSizeVerts);
#endif

        VertexXYToTexelXY(localX, localY, out var texelX, out var texelY);

        var weightData = GetWeightmapTextureData(layerIdx, out var outData);

        // if (!GetWeightMapIndex(layerInfo, false, out var LayerIdx)) {
        // 	return 0;
        // 	throw new ArgumentOutOfRangeException(nameof(layerInfo), "LayerInfo not found");
        // }

        var componentWeightmapLayerAllocations = Component.GetWeightmapLayerAllocations();
        var weightmapTexture = Component.GetWeightmapTextures()[componentWeightmapLayerAllocations[layerIdx].WeightmapTextureIndex];

        var weightmapStride = weightmapTexture.PlatformData.SizeX >> MipLevel;
        var weightmapComponentOffsetX = (int)((weightmapTexture.PlatformData.SizeX >> MipLevel) * Component.WeightmapScaleBias.Z);
        var weightmapComponentOffsetY = (int)((weightmapTexture.PlatformData.SizeY >> MipLevel) * Component.WeightmapScaleBias.W);

        if (weightData && outData != null) {
            return outData[texelX + weightmapComponentOffsetX + (texelY + weightmapComponentOffsetY) * weightmapStride];
            // return outData[texelX + texelY * weightmapSize];
        }

        return 0;
    }

    // public void GetHeightData(int LocalX, int LocalY, out float Height, out bool bHasHeight)
    // {
    // 	int TexelX = 0, TexelY = 0;
    // 	VertexXYToTexelXY(LocalX, LocalY, ref TexelX, ref TexelY);
    // }

    void VertexXYToTexelXY(int VertX, int VertY, out int OutX, out int OutY) {
        ComponentXYToSubsectionXY(VertX, VertY, out var SubNumX, out var SubNumY, out var SubX, out var SubY);

        OutX = SubNumX * SubsectionSizeVerts + SubX;
        OutY = SubNumY * SubsectionSizeVerts + SubY;
    }

    void ComponentXYToSubsectionXY(int compX, int compY, out int subNumX, out int subNumY, out int subX, out int subY) {
        // We do the calculation as if we're looking for the previous vertex.
        // This allows us to pick up the last shared vertex of every subsection correctly.
        subNumX = (compX - 1) / (SubsectionSizeVerts - 1);
        subNumY = (compY - 1) / (SubsectionSizeVerts - 1);
        subX = (compX - 1) % (SubsectionSizeVerts - 1) + 1;
        subY = (compY - 1) % (SubsectionSizeVerts - 1) + 1;

        // If we're asking for the first vertex, the calculation above will lead
        // to a negative SubNumX/Y, so we need to fix that case up.
        if (subNumX < 0) {
            subNumX = 0;
            subX = 0;
        }

        if (subNumY < 0) {
            subNumY = 0;
            subY = 0;
        }
    }

    internal void VertexIndexToXY(int vertexIndex, out int outX, out int outY) {
#if true //LANDSCAPE_VALIDATE_DATA_ACCESS
        Debug.Assert(MipLevel == 0);
#endif
        outX = vertexIndex % ComponentSizeVerts;
        outY = vertexIndex / ComponentSizeVerts;
    }

    ushort GetHeight(int vertexIndex) {
        VertexIndexToXY(vertexIndex, out var x, out var y);
        return GetHeight(x, y);
    }

    public ushort GetHeight(int localX, int localY) {
        FColor texel = GetHeightData(localX, localY);
        return (ushort)((texel.R << 8) + texel.G);
        // return (ushort)((texel->R << 8) + texel->G);
    }

    internal void XYtoVertexIndex(int vertX, int vertY, out int outVertexIndex) {
        outVertexIndex = vertY * ComponentSizeVerts + vertX;
    }

    internal FVector GetLocalVertex(int localX, int localY) {
        var scaleFactor = (float)Component.ComponentSizeQuads / (float)(ComponentSizeVerts - 1);
        GetXYOffset(localX, localY, out float xOffset, out float yOffset);
        return new FVector(localX * scaleFactor + xOffset, localY * scaleFactor + yOffset,
            GetLocalHeight(GetHeight(localX, localY)));
    }

    // same as GetLocalVertex but without the GetLocalHeight
    internal FVector GetVertex(int localX, int localY) {
        var scaleFactor = (float)Component.ComponentSizeQuads / (float)(ComponentSizeVerts - 1);
        GetXYOffset(localX, localY, out float xOffset, out float yOffset);
        return new FVector(localX * scaleFactor + xOffset, localY * scaleFactor + yOffset, GetHeight(localX, localY));
    }

    float GetLocalHeight(ushort height) {
        const float LANDSCAPE_ZSCALE = 1.0f / 128.0f;
        const int MaxValue = 65535;
        const float MidValue = 32768f;
        // Reserved 2 bits for other purpose
        // Most significant bit - Visibility, 0 is visible(default), 1 is invisible
        // 2nd significant bit - Triangle flip, not implemented yet
        return (height - MidValue) * LANDSCAPE_ZSCALE;
    }

    void GetXYOffset(int x, int y, out float xOffset, out float yOffset) {
        // if (XYOffsetMipData != null)
        // if (XYOffsetMipData) // false
        // {
        // 	FColor* Texel = GetXYOffsetData(X, Y);
        // 	XOffset = ((float)((Texel->R << 8) + Texel->G) - 32768.f) * LANDSCAPE_XYOFFSET_SCALE;
        // 	YOffset = ((float)((Texel->B << 8) + Texel->A) - 32768.f) * LANDSCAPE_XYOFFSET_SCALE;
        // }
        // else
        {
            xOffset = yOffset = 0.0f;
        }
    }

    void GetXYOffset(int vertexIndex, out float xOffset, out float yOffset) {
        VertexIndexToXY(vertexIndex, out var x, out var y);
        GetXYOffset(x, y, out xOffset, out yOffset);
    }

    void GetLocalTangentVectors(int localX, int localY, out FVector4 localTangentX, out FVector localTangentY,
        out FVector localTangentZ) {
        // Note: these are still pre-scaled, just not rotated

        FColor data = GetHeightData(localX, localY);
        // localTangentZ.X = 2.0f * data->B / 255.0f - 1.0f;
        localTangentZ.X = 2.0f * data.B / 255.0f - 1.0f;
        localTangentZ.Y = 2.0f * data.A / 255.0f - 1.0f;
        // localTangentZ.Y = 2.0f * data->A / 255.0f - 1.0f;
        localTangentZ.Z =
            (float)Math.Sqrt(1.0f - (localTangentZ.X * localTangentZ.X + localTangentZ.Y * localTangentZ.Y));
        localTangentX = new FVector4(-localTangentZ.Z, 0.0f, localTangentZ.X, 1.0f); // W=1.0??
        localTangentY = new FVector(0.0f, localTangentZ.Z, -localTangentZ.Y);
    }

    internal void GetLocalTangentVectors(int vertexIndex, out FVector4 localTangentX, out FVector localTangentY,
        out FVector localTangentZ) {
        VertexIndexToXY(vertexIndex, out var x, out var y);
        GetLocalTangentVectors(x, y, out localTangentX, out localTangentY, out localTangentZ);
    }
}
