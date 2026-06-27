using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Objects.Unversioned;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.GameTypes.Gothic1R.Assets.Objects;

public class FAlkimiaLightweightStaticMeshProxyDesc : FStructFallback
{
    public FAlkimiaLightweightStaticMeshProxyDesc(FAssetArchive Ar) : base()
    {
        Ar.Position += 96;
        Properties.AddRange(new FStructFallback(Ar, "AlkimiaLightweightStaticMeshProxyDesc", FRawHeader.FullRead, ReadType.RAW).Properties);
    }
}
