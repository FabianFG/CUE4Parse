using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Groom;

public class UGroomBindingAsset : UObject
{
    /* UGroomAsset::EGroomClassStripFlags */
    public const uint CDSF_ImportedStrands = 1;
    public const uint CDSF_MinLodData = 2;
    public const uint CDSF_StrandsStripped = 4;
    public const uint CDSF_CardsStripped = 8;
    public const uint CDSF_MeshesStripped = 16;

    public uint StripFlags;
    public FHairGroupBulkData[]? HairGroupBulkDatas;
    public FHairGroupData[]? HairGroupDatas;

    public static bool UsesEarlyAccessSerialization(EGame game) => game is < GAME_UE5_0 or GAME_UE5_EA_Legacy or GAME_UE5_EA;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (UsesEarlyAccessSerialization(Ar.Game))
        {
            HairGroupDatas = Ar.ReadArray(() => new FHairGroupData(Ar));
        }
        else
        {
            StripFlags = Ar.Read<uint>();
            HairGroupBulkDatas = Ar.ReadArray(() => new FHairGroupBulkData(Ar, StripFlags));
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (HairGroupDatas != null)
        {
            writer.WritePropertyName(nameof(HairGroupDatas));
            serializer.Serialize(writer, HairGroupDatas);
            return;
        }

        writer.WritePropertyName(nameof(StripFlags));
        writer.WriteValue(StripFlags);

        writer.WritePropertyName(nameof(HairGroupBulkDatas));
        serializer.Serialize(writer, HairGroupBulkDatas);
    }
}
