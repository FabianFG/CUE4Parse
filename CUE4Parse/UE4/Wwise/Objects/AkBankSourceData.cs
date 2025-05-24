using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkBankSourceData
{
    public readonly uint PluginId;
    public readonly byte StreamType;
    public readonly uint DataIndex;
    public readonly uint SampleRate;
    public readonly uint FormatBits;
    public readonly uint SourceId;
    public readonly uint FileId;
    public readonly uint FileOffset;
    public readonly uint InMemoryMediaSize;
    public readonly uint CacheId;
    public readonly byte SourceBits;
    public readonly bool IsLanguageSpecific;
    public readonly bool HasSource;
    public readonly bool ExternallySupplied;
    public readonly bool Prefetch;
    public readonly bool NonCachable;

    public AkBankSourceData(FArchive Ar)
    {
        PluginId = Ar.Read<uint>();
        var pluginType = PluginId & 0x0F;

        StreamType = Ar.Read<byte>();

        if (WwiseVersions.Version <= 46)
        {
            if (WwiseVersions.Version <= 26)
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

        SourceId = Ar.Read<uint>();
        if (WwiseVersions.Version <= 26)
        {
            // Do nothing
        }
        else if (WwiseVersions.Version <= 88)
        {
            FileId = Ar.Read<uint>();
            if (StreamType != 1)
            {
                FileOffset = Ar.Read<uint>();
                InMemoryMediaSize = Ar.Read<uint>();
            }
        }
        else if (WwiseVersions.Version <= 150)
        {
            if (WwiseVersions.Version <= 89 || WwiseVersions.Version <= 112)
            {
                FileId = Ar.Read<uint>();
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
            CacheId = Ar.Read<uint>();
            InMemoryMediaSize = Ar.Read<uint>();
        }

        if (WwiseVersions.Version <= 112)
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
        if (WwiseVersions.Version <= 26)
        {
            hasParam = true;
            alwaysParam = true;
        }
        else if (WwiseVersions.Version <= 126)
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
            WwisePlugin.ParsePluginParams(Ar, PluginId, alwaysParam);
        }
    }
}

