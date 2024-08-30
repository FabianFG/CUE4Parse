using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Engine.Curves;

[JsonConverter(typeof(FCurveMetaDataConverter))]
public class FCurveMetaData
{
    public readonly FAnimCurveType Type;
    public readonly FName[] LinkedBones;
    public readonly int MaxLOD;

    public FCurveMetaData(FArchive Ar, FAnimPhysObjectVersion.Type FrwAniVer)
    {
        Type = new FAnimCurveType(Ar);

        if (Ar.Game == EGame.GAME_TheFirstDescendant) Ar.Position += 4;
        LinkedBones = Ar.ReadArray(Ar.ReadFName);

        if (FrwAniVer >= FAnimPhysObjectVersion.Type.AddLODToCurveMetaData)
        {
            MaxLOD = Ar.Game == EGame.GAME_KingdomHearts3 ? Ar.Read<int>() : Ar.Read<byte>();
        }

        if (Ar.Game == EGame.GAME_FinalFantasy7Remake)
        {
            // Cutscene mat replacements
            var matAssetName = Ar.ReadFName();
            var matName = Ar.ReadFName();
        }
    }
}
