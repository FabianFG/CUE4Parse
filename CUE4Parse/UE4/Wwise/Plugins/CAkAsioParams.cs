using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkAsioSinkParams(FArchive Ar) : IAkPluginParam
{
    public AkChannelConfig ChannelConfig = Ar.Read<AkChannelConfig>();
    public int BaseChannel = Ar.Read<int>();
}

public class CAkAsioSourceParams(FArchive Ar) : IAkPluginParam
{
    public AkChannelConfig ChannelConfig = Ar.Read<AkChannelConfig>();
    public int BaseChannel = Ar.Read<int>();
}
