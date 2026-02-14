using CUE4Parse.GameTypes.Borderlands4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Animation;

public class UFaceFXAnimSet : UObject
{
    public GbxFaceFXAnimData[] FaceFXAnimDataList = [];
    [JsonIgnore]
    public byte[] AnimBuffer = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Game is not EGame.GAME_Borderlands4)
            return;

        FaceFXAnimDataList = GetOrDefault<GbxFaceFXAnimData[]>(nameof(FaceFXAnimDataList)) ?? [];
        AnimBuffer = GetOrDefault<byte[]>(nameof(AnimBuffer));
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
    }
}
