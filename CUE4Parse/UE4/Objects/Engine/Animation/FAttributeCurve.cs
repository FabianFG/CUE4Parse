using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;

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
        var assetPath = ScriptStructPath.AssetPathName;

        if (assetPath.IsNone)
            return;

        if( assetPath.Text.StartsWith("/Script"))
        {
            var ScriptStructType = ScriptStructPath.AssetPathName.Text.SubstringAfterLast('.');
            Values = new FStructFallback[Keys.Length];
            for (var i = 0; i < Keys.Length; i++)
            {
                Values[i] = new FStructFallback(Ar, ScriptStructType);
            }
        }
        else
        {
            throw new ParserException("Asset ScriptStruct for FAttributeCurve isn't supported yet");
        }
    }
}
