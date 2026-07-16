using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Exports.Material.Parameters;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.GameTypes.RocoKingdomWorld.Assets.Objects;

public class FNiagaraEventGeneratorProperties(FAssetArchive Ar) : IUStruct
{
    public int MaxEventsPerFrame = Ar.Read<int>();
    public FName ID = Ar.ReadFName();
    public FStructFallback DataSetCompiledData = new FStructFallback(Ar, "NiagaraDataSetCompiledData");
}

public class FRKWStaticSwitchParameter : FStaticSwitchParameter
{
    public bool bDynamicChange;

    public FRKWStaticSwitchParameter(FArchive Ar) : base(Ar)
    {
        bDynamicChange = Ar.ReadBoolean();
    }
}
