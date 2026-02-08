using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;

namespace CUE4Parse.UE4.Wwise.Plugins;

internal class CAkChannelRouterFXParams(FArchive Ar) : IAkPluginParam
{
    public EAkChannelConfig BusChannelConfig = Ar.Read<EAkChannelConfig>();
}
