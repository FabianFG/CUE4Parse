using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkChannelConfig
{
    public readonly byte NumChannels;
    public readonly EAkChannelConfigType ConfigType;
    public readonly uint ChannelMask;

    public AkChannelConfig(FArchive Ar)
    {
        var data = Ar.Read<uint>();
        NumChannels = (byte) (data & 0xFF);
        ConfigType = (EAkChannelConfigType) ((data >> 8) & 0x0F);
        ChannelMask = (data >> 12) & 0xFFFFF;
    }
}
