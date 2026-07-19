using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Assets.Exports.FastGeoStreaming;

public class FFastGeoHLOD : FFastGeoComponentCluster
{
    public bool bRequireWarmup;
    public FGuid SourceCellGuid;
    public FGuid StandaloneHLODGuid;
    public FGuid? CustomHLODGuid;

    public FFastGeoHLOD(FFastGeoArchive Ar) : base(Ar)
    {
        bRequireWarmup = Ar.ReadBoolean();
        SourceCellGuid = Ar.Read<FGuid>();
        StandaloneHLODGuid = Ar.Read<FGuid>();
        if (Ar.Game is >= GAME_UE5_7) CustomHLODGuid = Ar.Read<FGuid>();
    }
}
