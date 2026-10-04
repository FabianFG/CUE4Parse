using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Engine.Font;

public class FFontPage
{
    public FPackageIndex[] Textures;
    public FFontCharacter[] Characters;

    public FFontPage(FAssetArchive Ar)
    {
        Textures = Ar.ReadArray(() => new FPackageIndex(Ar));
        Characters = Ar.ReadArray(() => new FFontCharacter(Ar));
    }

    public FFontPage(FPackageIndex[] textures, FFontCharacter[] characters)
    {
        Textures = textures;
        Characters = characters;
    }
}
