using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Parameters;

public class FRangeDesc
{
    [JsonIgnore] public int Version = 3;
    public string Name;
    public string UID;
    public int DimensionParameter = -1;

    public FRangeDesc(FMutableArchive Ar)
    {
        if (Ar.Game < EGame.GAME_UE5_6) Version = Ar.Read<int>();
        Name = Ar.Game >= EGame.GAME_UE5_4 ? Ar.ReadFString() : Ar.ReadString();
        UID = Ar.Game >= EGame.GAME_UE5_4 ? Ar.ReadFString() : Ar.ReadString();
        if (Version >= 2)
            DimensionParameter = Ar.Read<int>();
    }
}
