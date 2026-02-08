using System;
using System.Runtime.InteropServices;
using System.Text;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkRecorderFXParams(FArchive Ar, int size) : IAkPluginParam
{
    public AkRecorderFXParams Params = new AkRecorderFXParams(Ar, size);
}

public struct AkRecorderFXParams(FArchive Ar, int size)
{
    public AkRecorderRTPCParams RTPC = Ar.Read<AkRecorderRTPCParams>();
    public AkRecorderNonRTPCParams NonRTPC = new AkRecorderNonRTPCParams(Ar, size-20);

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct AkRecorderRTPCParams
    {
        public float fCenter;
        public float fFront;
        public float fSurround;
        public float fRear;
        public float fLFE;
    }

    public struct AkRecorderNonRTPCParams
    {
        public short iFormat;
        public string szFilename;
        public bool bDownmixToStereo;
        public bool bApplyDownstreamVolume;

        const int _MaxCount = 0x103;

        public AkRecorderNonRTPCParams(FArchive Ar, int size)
        {
            iFormat = Ar.Read<short>();
            var len = Math.Min((size - 2) >> 1, _MaxCount + 1);
            var saved = Ar.Position;
            var data = Ar.ReadArray<char>(len);
            len = Array.IndexOf(data, 0) + 1;
            Ar.Position = saved;
            szFilename = Encoding.Unicode.GetString(Ar.ReadBytes(len * 2), 0, (len - 1) * 2);
            bDownmixToStereo = Ar.Read<byte>() != 0;
            bApplyDownstreamVolume = Ar.Read<byte>() != 0;
        }
    }
}
