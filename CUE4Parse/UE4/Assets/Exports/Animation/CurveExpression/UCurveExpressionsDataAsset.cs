using System.Collections.Generic;
using CommunityToolkit.HighPerformance;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Animation.CurveExpression;

public class UCurveExpressionsDataAsset : UObject
{
    public FName[] NamedConstants;
    public FExpressionData ExpressionData;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        
        if (FCurveExpressionCustomVersion.Get(Ar) >= FCurveExpressionCustomVersion.Type.ExpressionDataInSharedObject)
        {
            NamedConstants = Ar.ReadArray(Ar.ReadFName);
        }

        ExpressionData = new FExpressionData(Ar);
    }
}

public class FExpressionData
{
    public Dictionary<FName, FExpressionObject> ExpressionMap;
    
    public FExpressionData(FArchive Ar)
    {
        ExpressionMap = Ar.ReadMap(() => (Ar.ReadFName(), new FExpressionObject(Ar)));
    }
}