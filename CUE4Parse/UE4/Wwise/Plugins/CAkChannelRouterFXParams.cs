using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins;

internal class CAkChannelRouterFXParams(FArchive Ar) : IAkPluginParam
{
    public AkChannelConfig BusChannelConfig = Ar.Read<AkChannelConfig>();
}
