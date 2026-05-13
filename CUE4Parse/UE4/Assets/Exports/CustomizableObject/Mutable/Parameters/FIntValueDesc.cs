using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Parameters;

public class FIntValueDesc
{
    public short Value;
    public string Name;
    
    public FIntValueDesc(FMutableArchive Ar)
    {
        Value = Ar.Read<short>();
        Name = Ar.Game >= EGame.GAME_UE5_4 ? Ar.ReadFString() : Ar.ReadString();
    }
}
