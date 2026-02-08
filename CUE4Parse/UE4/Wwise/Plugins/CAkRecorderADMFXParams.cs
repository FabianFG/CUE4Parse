using System.Text;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkRecorderADMFXParams(FArchive Ar) : IAkPluginParam
{
    public AkRecorderADMFXParams Params = new AkRecorderADMFXParams(Ar);
}

public struct AkRecorderADMFXParams{
    public short Profile;
    public short ChannelCount;
    public EAkChannelConfig MainMixChannelConfig;
    public bool Passthrough;
    public bool PreserveExtraBeds;
    public bool ApplyDownstreamVolume;
    public bool Hold;
    public string GameFilename;

    public AkRecorderADMFXParams(FArchive Ar)
    {
        Profile = Ar.Read<short>();
        ChannelCount = Ar.Read<short>();
        MainMixChannelConfig = Ar.Read<EAkChannelConfig>();
        Passthrough = Ar.Read<byte>() != 0;
        PreserveExtraBeds = Ar.Read<byte>() != 0;
        ApplyDownstreamVolume = Ar.Read<byte>() != 0;
        Hold = Ar.Read<byte>() != 0;
        GameFilename = Encoding.Unicode.GetString(Ar.ReadBytes(0x104*2));
    }
}
