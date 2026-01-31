using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;

namespace CUE4Parse.UE4.Wwise.Objects;

public class GlobalSettings
{
    public readonly EAkFilterBehavior FilterBehavior;
    public readonly float VolumeThreshold;
    public readonly ushort MaxNumVoicesLimitInternal;
    public readonly ushort MaxNumDangerousVirtVoicesLimitInternal;

    public GlobalSettings(FArchive Ar)
    {
        if (WwiseVersions.Version > 140)
        {
            FilterBehavior = Ar.Read<EAkFilterBehavior>();
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
