using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.FastGeoStreaming;

public class UFastGeoContainer : UAssetUserData
{
    public FFastGeoComponentCluster[] ComponentClusters;
    public FFastGeoHLOD[] HLODs;
    public FPackageIndex[] Assets;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        Assets = GetOrDefault<FPackageIndex[]>(nameof(Assets), []);

        var length = (int)(validPos - Ar.Position);
        if (Ar.Game is GAME_WutheringWaves)
        {
            using var arclone = (FAssetArchive) Ar.Clone();
            arclone.Versions = (VersionContainer) Ar.Versions.Clone();
            arclone.Game = GAME_WutheringWavesFastGeo;
            using var fgWuWaAr = new FFastGeoArchive(arclone, Assets);
            ComponentClusters = fgWuWaAr.ReadArray(() => new FFastGeoComponentCluster(fgWuWaAr));
            HLODs = fgWuWaAr.ReadArray(() => new FFastGeoHLOD(fgWuWaAr));
            Ar.Position = arclone.Position;
            return;
        }
        using var fgAr = new FFastGeoArchive(Ar, Assets);
        ComponentClusters = fgAr.ReadArray(() => new FFastGeoComponentCluster(fgAr));
        HLODs = fgAr.ReadArray(() => new FFastGeoHLOD(fgAr));
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(ComponentClusters));
        serializer.Serialize(writer, ComponentClusters);
        writer.WritePropertyName(nameof(HLODs));
        serializer.Serialize(writer, HLODs);
    }
}
