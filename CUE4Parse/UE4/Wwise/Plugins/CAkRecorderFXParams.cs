using System;
using System.Runtime.InteropServices;
using System.Text;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkRecorderFXParams(FWwiseArchive Ar, int size) : IAkPluginParam
{
    public AkRecorderFXParams Params = new(Ar, size);
}

public struct AkRecorderFXParams(FWwiseArchive Ar, int size)
{
    public AkRecorderRTPCParams RTPC = Ar.Read<AkRecorderRTPCParams>();
    public AkRecorderNonRTPCParams NonRTPC = new(Ar, size - 20);

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct AkRecorderRTPCParams
    {
        public float Center;
        public float Front;
        public float Surround;
        public float Rear;
        public float LFE;
    }

    public struct AkRecorderNonRTPCParams
    {
        public short Format;
        public string Filename;
        public bool DownmixToStereo;
        public bool ApplyDownstreamVolume;
        public short AmbisonicsChannelOrdering;

        const int _MaxCount = 0x103;

        public AkRecorderNonRTPCParams(FWwiseArchive Ar, int size)
        {
            Format = Ar.Read<short>();
            var len = Math.Min((size - 2), _MaxCount);
            var saved = Ar.Position;
            var data = Ar.ReadArray<byte>(len);
            len = Array.IndexOf(data, (byte) 0) + 1;
            Ar.Position = saved;
            Filename = len > 0 ? Encoding.UTF8.GetString(Ar.ReadBytes(len), 0, len - 1) : "";
            DownmixToStereo = Ar.Read<byte>() != 0;
            ApplyDownstreamVolume = Ar.Read<byte>() != 0;
            if (Ar.Version >= 134)
                AmbisonicsChannelOrdering = Ar.Read<short>();
        }
    }
}
