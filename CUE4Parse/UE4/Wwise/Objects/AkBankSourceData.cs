using System.Collections.Generic;
using System;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkBankSourceData
{
    public uint PluginId { get; private set; }
    public byte StreamType { get; private set; }
    public uint DataIndex { get; private set; }
    public uint SampleRate { get; private set; }
    public uint FormatBits { get; private set; }
    public uint SourceID { get; private set; }
    public uint FileID { get; private set; }
    public uint FileOffset { get; private set; }
    public uint InMemoryMediaSize { get; private set; }
    public uint CacheID { get; private set; }
    public byte SourceBits { get; private set; }
    public bool IsLanguageSpecific { get; private set; }
    public bool HasSource { get; private set; }
    public bool ExternallySupplied { get; private set; }
    public bool Prefetch { get; private set; }
    public bool NonCachable { get; private set; }

    public AkBankSourceData(FArchive Ar)
    {
        PluginId = Ar.Read<uint>();
        var pluginType = PluginId & 0x0F;

        StreamType = Ar.Read<byte>();

        if (WwiseVersions.WwiseVersion <= 46)
        {
            if (WwiseVersions.WwiseVersion <= 26)
            {
                DataIndex = Ar.Read<uint>();
                SampleRate = Ar.Read<uint>();
                FormatBits = Ar.Read<uint>();
            }
            else
            {
                SampleRate = Ar.Read<uint>();
                FormatBits = Ar.Read<uint>();
            }
        }

        SourceID = Ar.Read<uint>();
        if (WwiseVersions.WwiseVersion <= 26)
        {
            // Do nothing
        }
        else if (WwiseVersions.WwiseVersion <= 88)
        {
            FileID = Ar.Read<uint>();
            if (StreamType != 1)
            {
                FileOffset = Ar.Read<uint>();
                InMemoryMediaSize = Ar.Read<uint>();
            }
        }
        else if (WwiseVersions.WwiseVersion <= 150)
        {
            if (WwiseVersions.WwiseVersion <= 89 || WwiseVersions.WwiseVersion <= 112)
            {
                FileID = Ar.Read<uint>();
                if (StreamType != 2)
                    FileOffset = Ar.Read<uint>();
                InMemoryMediaSize = Ar.Read<uint>();
            }
            else
            {
                InMemoryMediaSize = Ar.Read<uint>();
            }
        }
        else
        {
            CacheID = Ar.Read<uint>();
            InMemoryMediaSize = Ar.Read<uint>();
        }

        if (WwiseVersions.WwiseVersion <= 112)
        {
            SourceBits = Ar.Read<byte>();
            IsLanguageSpecific = (SourceBits & (1 << 0)) != 0;
            HasSource = (SourceBits & (1 << 1)) != 0;
            ExternallySupplied = (SourceBits & (1 << 2)) != 0;
        }
        else
        {
            SourceBits = Ar.Read<byte>();
            IsLanguageSpecific = (SourceBits & (1 << 0)) != 0;
            Prefetch = (SourceBits & (1 << 1)) != 0;
            NonCachable = (SourceBits & (1 << 3)) != 0;
            HasSource = (SourceBits & (1 << 7)) != 0;
        }

        bool hasParam;
        bool alwaysParam;
        if (WwiseVersions.WwiseVersion <= 26)
        {
            hasParam = true;
            alwaysParam = true;
        }
        else if (WwiseVersions.WwiseVersion <= 126)
        {
            hasParam = (pluginType == 2 || pluginType == 5);
            alwaysParam = false;
        }
        else
        {
            hasParam = (pluginType == 2);
            alwaysParam = false;
        }

        if (hasParam)
        {
            ParsePluginParams(Ar, PluginId, alwaysParam);
        }
    }

    private void ParsePluginParams(FArchive Ar, uint pluginId, bool always)
    {
        if (pluginId == 0)
            return;

        if ((int) pluginId < 0 && !always)
            return;

        uint size = Ar.Read<uint>();
        if (size == 0)
            return;

        if (PluginDispatch.TryGetValue(pluginId, out var dispatch))
        {
            dispatch(Ar, size);
        }
        else
        {
            ParseChunkDefault(Ar, size);
        }
    }

    private void ParseChunkDefault(FArchive Ar, uint size)
    {
        // Skip the size of the chunk (gap)
        Ar.Position += size;
    }

    // TODO: add plugins
    private static readonly Dictionary<uint, Action<FArchive, uint>> PluginDispatch = new Dictionary<uint, Action<FArchive, uint>>
    {
        //{ 0x00640002, CAkFxSrcSine },
        //{ 0x00650002, CAkFxSrcSilence },
        //{ 0x00660002, CAkToneGen },
        //{ 0x00690003, CAkParameterEQFX },
        //{ 0x006A0003, CAkDelayFX },
        //{ 0x006E0003, CAkPeakLimiterFX },
        //{ 0x00730003, CAkFDNReverbFX },
        //{ 0x00760003, CAkRoomVerbFX },
        //{ 0x007D0003, CAkFlangerFX },
        //{ 0x007E0003, CAkGuitarDistortionFX },
        //{ 0x007F0003, CAkConvolutionReverbFX },
        //{ 0x00810003, CAkMeterFX },
        //{ 0x00870003, CAkStereoDelayFX },
        //{ 0x008B0003, CAkGainFX },
        //{ 0x008A0003, CAkHarmonizerFX },
        //{ 0x00940002, CAkSynthOne },
        //{ 0x00C80002, CAkFxSrcAudioInput },
        //{ 0x00041033, iZTrashDelayFXParams }
    };
}

