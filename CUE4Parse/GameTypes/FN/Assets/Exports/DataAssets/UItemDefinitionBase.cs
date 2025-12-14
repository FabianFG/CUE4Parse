using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.GameTypes.FN.Assets.Exports.DataAssets;

public class UItemDefinitionBase : UMcpItemDefinitionBase
{
    public FInstancedStruct[] DataList;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        DataList = GetOrDefault<FInstancedStruct[]>(nameof(DataList), []);
    }
}
