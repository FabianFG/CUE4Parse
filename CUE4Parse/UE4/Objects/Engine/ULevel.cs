using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Engine;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FPrecomputedVisibilityCell : IUStruct
{
    public readonly FVector Min;
    public readonly ushort ChunkIndex;
    public readonly ushort DataOffset;

    public FPrecomputedVisibilityCell(FAssetArchive Ar)
    {
        Min = new FVector(Ar);
        ChunkIndex = Ar.Read<ushort>();
        DataOffset = Ar.Read<ushort>();
    }
}

[JsonConverter(typeof(FCompressedVisibilityChunkConverter))]
public readonly struct FCompressedVisibilityChunk : IUStruct
{
    public readonly bool bCompressed;
    public readonly int UncompressedSize;
    public readonly byte[] Data;

    public FCompressedVisibilityChunk(FAssetArchive Ar)
    {
        bCompressed = Ar.ReadBoolean();
        UncompressedSize = Ar.Read<int>();
        Data = Ar.ReadBytes(Ar.Read<int>());
    }
}

public readonly struct FPrecomputedVisibilityBucket : IUStruct
{
    public readonly int CellDataSize;
    public readonly FPrecomputedVisibilityCell[] Cells;
    public readonly FCompressedVisibilityChunk[] CellDataChunks;

    public FPrecomputedVisibilityBucket(FAssetArchive Ar)
    {
        CellDataSize = Ar.Read<int>();
        Cells = Ar.Game != EGame.GAME_MetroAwakening ? Ar.ReadArray<FPrecomputedVisibilityCell>() : Ar.ReadArray(() => new FPrecomputedVisibilityCell(Ar));
        CellDataChunks = Ar.ReadArray(() => new FCompressedVisibilityChunk(Ar));
    }
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct FPrecomputedVisibilityHandler : IUStruct
{
    public readonly FVector2D PrecomputedVisibilityCellBucketOriginXY;
    public readonly float PrecomputedVisibilityCellSizeXY;
    public readonly float PrecomputedVisibilityCellSizeZ;
    public readonly int PrecomputedVisibilityCellBucketSizeXY;
    public readonly int PrecomputedVisibilityNumCellBuckets;
    public readonly FPrecomputedVisibilityBucket[] PrecomputedVisibilityCellBuckets;

    public FPrecomputedVisibilityHandler(FAssetArchive Ar)
    {
        PrecomputedVisibilityCellBucketOriginXY = new FVector2D(Ar);
        PrecomputedVisibilityCellSizeXY = Ar.Read<float>();
        PrecomputedVisibilityCellSizeZ = Ar.Read<float>();
        PrecomputedVisibilityCellBucketSizeXY = Ar.Read<int>();
        PrecomputedVisibilityNumCellBuckets = Ar.Read<int>();
        PrecomputedVisibilityCellBuckets = Ar.ReadArray(() => new FPrecomputedVisibilityBucket(Ar));
    }
}

public readonly struct FPrecomputedVolumeDistanceField : IUStruct
{
    public readonly float VolumeMaxDistance;
    public readonly FBox VolumeBox;
    public readonly int VolumeSizeX;
    public readonly int VolumeSizeY;
    public readonly int VolumeSizeZ;
    public readonly FColor[] Data;

    public FPrecomputedVolumeDistanceField(FAssetArchive Ar)
    {
        VolumeMaxDistance = Ar.Read<float>();
        VolumeBox = new FBox(Ar);
        VolumeSizeX = Ar.Read<int>();
        VolumeSizeY = Ar.Read<int>();
        VolumeSizeZ = Ar.Read<int>();
        Data = Ar.ReadArray<FColor>();
    }
}

public class ULevel : Assets.Exports.UObject
{
    public FPackageIndex[] Actors { get; private set; }
    public FURL URL { get; private set; }
    public FPackageIndex Model { get; private set; }
    public FPackageIndex[] ModelComponents { get; private set; }
    public FPackageIndex LevelScriptActor { get; private set; }
    public FPackageIndex NavListStart { get; private set; }
    public FPackageIndex NavListEnd { get; private set; }
    public FPrecomputedVisibilityHandler PrecomputedVisibilityHandler { get; private set; }
    public FPrecomputedVolumeDistanceField PrecomputedVolumeDistanceField { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (Ar.Game == EGame.GAME_SeaOfThieves) Ar.Position += 4;
        Actors = Ar.ReadArray(() => new FPackageIndex(Ar));
        URL = new FURL(Ar);
        Model = new FPackageIndex(Ar);
        ModelComponents = Ar.ReadArray(() => new FPackageIndex(Ar));
        LevelScriptActor = new FPackageIndex(Ar);
        NavListStart = new FPackageIndex(Ar);
        NavListEnd = new FPackageIndex(Ar);
        if (Ar.Game == EGame.GAME_MetroAwakening && GetOrDefault<bool>("bIsLightingScenario")) return;
        PrecomputedVisibilityHandler = new FPrecomputedVisibilityHandler(Ar);
        PrecomputedVolumeDistanceField = new FPrecomputedVolumeDistanceField(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("Actors");
        serializer.Serialize(writer, Actors);

        writer.WritePropertyName("URL");
        serializer.Serialize(writer, URL);

        writer.WritePropertyName("Model");
        serializer.Serialize(writer, Model);

        writer.WritePropertyName("ModelComponents");
        serializer.Serialize(writer, ModelComponents);

        writer.WritePropertyName("LevelScriptActor");
        serializer.Serialize(writer, LevelScriptActor);

        writer.WritePropertyName("NavListStart");
        serializer.Serialize(writer, NavListStart);

        writer.WritePropertyName("NavListEnd");
        serializer.Serialize(writer, NavListEnd);

        writer.WritePropertyName("PrecomputedVisibilityHandler");
        serializer.Serialize(writer, PrecomputedVisibilityHandler);

        writer.WritePropertyName("PrecomputedVolumeDistanceField");
        serializer.Serialize(writer, PrecomputedVolumeDistanceField);
    }
}
