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

        if (Ar.Game is EGame.GAME_Borderlands4)
        {
            FaceFXAnimDataList = GetOrDefault<GbxFaceFXAnimData[]>(nameof(FaceFXAnimDataList)) ?? [];
            AnimBuffer = GetOrDefault<byte[]>(nameof(AnimBuffer));
            return;
        }

        Ar.SkipMultipleFixedArrays(Ar.Read<int>(), 1); // RawFaceFXAnimSetBytes
        Ar.SkipMultipleFixedArrays(Ar.Read<int>(), 1); // RawFaceFXMiniSessionBytes
    }
}
