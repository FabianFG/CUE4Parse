using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.Engine.Animation;

public readonly struct FAttributeKey
{
    public readonly float Time;
}

public struct FAttributeCurve : IUStruct
{
    public FAttributeKey[] Keys;
    public FSoftObjectPath ScriptStructPath;
    public FStructFallback[] Values;

    public FAttributeCurve(FAssetArchive Ar)
    {
        Keys = Ar.ReadArray<FAttributeKey>();
        ScriptStructPath = new FSoftObjectPath(Ar);
        var assetPath = ScriptStructPath.ToString();

        if (ScriptStructPath.AssetPathName.IsNone)
            return;

        if (assetPath.StartsWith("/Script", StringComparison.Ordinal))
        {
            Values = new FStructFallback[Keys.Length];
            for (var i = 0; i < Keys.Length; i++)
            {
                Values[i] = new FStructFallback(Ar, assetPath);
            }
        }
        else
        {
            throw new ParserException("Asset ScriptStruct for FAttributeCurve isn't supported yet");
        }
    }
}
