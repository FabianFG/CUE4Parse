using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.GameTypes.ACE8.Assets.Exports;

public class ULuaScript : UObject
{
    public string Code;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        Code = GetOrDefault<string>(nameof(Code));
    }
}
