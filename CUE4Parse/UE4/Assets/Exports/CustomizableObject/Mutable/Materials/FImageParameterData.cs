using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Images;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Materials;

public class FImageParameterData
{
    public uint? Address;
    public FImage? ImageParameter;
    public int ImagePropertyIndex;

    public FImageParameterData(FMutableArchive Ar)
    {
        var type = Ar.Read<byte>();
        switch (type)
        {
            case 0:
                Address = Ar.Read<uint>();
                break;
            case 1:
                ImageParameter = Ar.ReadPtr(() => new FImage(Ar));
                break;
            default:
                throw new Exception($"Unknown FImageParameterData type {type}");
        }

        ImagePropertyIndex = Ar.Game >= GAME_UE5_8 ? Ar.Read<int>() : 0;
    }
}
