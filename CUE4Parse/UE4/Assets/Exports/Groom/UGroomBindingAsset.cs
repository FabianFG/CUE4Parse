using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Groom;

public class UGroomBindingAsset : UObject
{
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
            var StripFlags = Ar.Read<EGroomClassStripFlags>();
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

        writer.WritePropertyName(nameof(HairGroupBulkDatas));
        serializer.Serialize(writer, HairGroupBulkDatas);
    }
}

/* UGroomAsset::EGroomClassStripFlags */
[Flags]
public enum EGroomClassStripFlags : uint
{
    CDSF_ImportedStrands = 1,
    CDSF_MinLodData = 2,
    CDSF_StrandsStripped = 4,
    CDSF_CardsStripped = 8,
    CDSF_MeshesStripped = 16
}