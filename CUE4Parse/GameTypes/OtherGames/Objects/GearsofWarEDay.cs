using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;

namespace CUE4Parse.GameTypes.OtherGames.Objects;

public class FTCNamedParam : IUStruct
{
    public bool bHasDefaultValue;
    public FName ParamName;
    public FName Key;
    public object Value;

    public FTCNamedParam(FAssetArchive Ar, string name)
    {
        bHasDefaultValue = Ar.ReadBoolean();
        ParamName = Ar.ReadFName();
        Key = Ar.ReadFName();
        Value = name.SubstringAfterLast('_') switch
        {
            "Bool" => Ar.ReadBoolean(),
            "Vector" => new FVector(Ar),
            "Vector2D" => new FVector2D(Ar),
            "Float" => Ar.Read<float>(),
            "LinearColor" => Ar.Read<FLinearColor>(),
            _ => new FPackageIndex(Ar)
        };
        _ = Ar.ReadBoolean();
    }
}
