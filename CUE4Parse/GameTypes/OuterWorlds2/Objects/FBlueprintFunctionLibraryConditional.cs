using CUE4Parse.GameTypes.OuterWorlds2.Properties;
using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.GameTypes.OuterWorlds2.Objects;

public class FBlueprintFunctionLibraryConditional : IUStruct
{
    public FStructFallback FunctionReference;
    public FStructFallback Parameters;

    public FBlueprintFunctionLibraryConditional(FAssetArchive Ar, bool readType)
    {
        if (readType) Ar.Position += 1; 
        Ar.Position += 1;
        FunctionReference = new FStructFallback(Ar, "MemberReference");
        Parameters = new FStructFallback();
        Parameters.Properties.AddRange(Ar.ReadArray(() => new FOW2FPropertyTag(Ar)));
    }
}
