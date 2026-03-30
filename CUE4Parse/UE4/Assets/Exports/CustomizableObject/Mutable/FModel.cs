using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;

public class FModel
{
    [JsonIgnore] public int Version;
    public FProgram Program;

    public FModel(FMutableArchive Ar)
    {
        if (Ar.Game < EGame.GAME_UE5_6) Version = Ar.Read<int>();
        Program = new(Ar);
    }
}
