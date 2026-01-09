using System.Collections.Generic;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.PCG;

public class UPCGLandscapeCache : UObject
{
    public Dictionary<CacheMapKey, FPCGLandscapeCacheEntry> CachedData = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        CachedData = Ar.ReadMap(Ar.Read<CacheMapKey>, () => new FPCGLandscapeCacheEntry(Ar));
    }

    override protected internal void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(CachedData));
        serializer.Serialize(writer, CachedData);
    }
}

public class FPCGLandscapeCacheEntry
{
    public FVector PointHalfSize;
    public int Stride;
    public FName[] LayerDataNames;
    [JsonIgnore] public FVector[] PositionsAndNormals;
    [JsonIgnore] public byte[][] LayerData;

    public FPCGLandscapeCacheEntry(FAssetArchive Ar)
    {
        if (Ar.Game < EGame.GAME_UE5_4) _ = new FPackageIndex(Ar); // DummyComponent
        PointHalfSize = new FVector(Ar);
        Stride = Ar.Read<int>();
        LayerDataNames = Ar.ReadArray(Ar.ReadFName);
        var BulkData = new FByteBulkData(Ar);
        using var reader = new FByteArchive("FPCGLandscapeCacheEntry", BulkData.Data, Ar.Versions);
        PositionsAndNormals = reader.ReadArray(() => new FVector(reader));
        LayerData = reader.ReadArray(() => reader.ReadArray<byte>());
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct CacheMapKey
{
    public FGuid LandscapeGuid;
    public FIntPoint Coordinate;
    //FObjectKey WorldKey;
}

[StructLayout(LayoutKind.Sequential)]
public struct FObjectKey
{
    public int ObjectIndex;
    public int ObjectSerialNumber;
}
