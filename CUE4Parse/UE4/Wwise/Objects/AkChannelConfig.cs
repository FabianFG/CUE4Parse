using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;

namespace CUE4Parse.UE4.Wwise.Objects;

// Can be either this or directly in EAkChannelConfig form
// https://www.audiokinetic.com/en/public-library/2025.1.4_9062/?source=SDK&id=struct_ak_channel_config.html
public readonly struct AkChannelConfig
{
    public readonly byte NumChannels;
    public readonly EAkChannelConfig ConfigTypePacked;
    public readonly EAkChannelConfigType ConfigType;
    public readonly uint ChannelMask;

    public AkChannelConfig(FArchive Ar)
    {
        var data = Ar.Read<uint>();
        NumChannels = (byte) (data & 0xFF);
        ConfigTypePacked = (EAkChannelConfig) data;
        ConfigType = (EAkChannelConfigType) ((data >> 8) & 0x0F);
        ChannelMask = (data >> 12) & 0xFFFFF;
    }
}
