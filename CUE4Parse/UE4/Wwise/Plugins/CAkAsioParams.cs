using CUE4Parse.UE4.Wwise.Enums;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkAsioSinkParams(FWwiseArchive Ar) : IAkPluginParam
{
    public EAkChannelConfig ChannelConfig = Ar.Read<EAkChannelConfig>();
    public int BaseChannel = Ar.Read<int>();
}

public class CAkAsioSourceParams(FWwiseArchive Ar) : IAkPluginParam
{
    public EAkChannelConfig ChannelConfig = Ar.Read<EAkChannelConfig>();
    public int BaseChannel = Ar.Read<int>();
}
