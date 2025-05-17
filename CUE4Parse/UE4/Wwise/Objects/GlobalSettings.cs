using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;

namespace CUE4Parse.UE4.Wwise.Objects;

public class GlobalSettings
{
    public EFilterBehavior FilterBehavior { get; }
    public float VolumeThreshold { get; }
    public ushort MaxNumVoicesLimitInternal { get; }
    public ushort MaxNumDangerousVirtVoicesLimitInternal { get; }

    public GlobalSettings(FArchive Ar)
    {
        if (WwiseVersions.Version > 140)
        {
            FilterBehavior = Ar.Read<EFilterBehavior>();
        }

        VolumeThreshold = Ar.Read<float>();

        if (WwiseVersions.Version > 53)
        {
            MaxNumVoicesLimitInternal = Ar.Read<ushort>();
        }

        if (WwiseVersions.Version > 126)
        {
            MaxNumDangerousVirtVoicesLimitInternal = Ar.Read<ushort>();
        }
    }
}
