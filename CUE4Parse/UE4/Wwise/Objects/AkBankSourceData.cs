using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using CUE4Parse.UE4.Wwise.Enums.Flags;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects;

// CAkBankMgr::LoadSource
public class AkBankSourceData
{
    public readonly EAkPluginId PluginId;
    public readonly EAkPluginType PluginType;
    public readonly EAKBKSourceType SourceType;
    public readonly uint DataIndex;
    public readonly uint SampleRate;
    public readonly uint FormatBits;
    public readonly uint SourceId;
    public readonly uint FileId;
    public readonly uint FileOffset;
    public readonly uint InMemoryMediaSize;
    public readonly uint CacheId;
    public readonly EBankSourceFlags BankSourceFlags;
    public readonly bool HasPluginParams;
    public readonly object? PluginParams;

    public AkBankSourceData(FArchive Ar)
    {
        var rawPluginId = Ar.Read<uint>();
        PluginId = (EAkPluginId) rawPluginId;
        PluginType = (EAkPluginType) (rawPluginId & 0x0F);
        SourceType = Ar.Read<EAKBKSourceType>();

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
        switch (WwiseVersions.Version)
        {
            case <= 26:
                // Do nothing
                break;
            case <= 88:
                FileId = Ar.Read<uint>();
                if (SourceType is not EAKBKSourceType.PrefetchStreaming)
                {
                    FileOffset = Ar.Read<uint>();
                    InMemoryMediaSize = Ar.Read<uint>();
                }
                break;
            case <= 150:
                if (WwiseVersions.Version <= 112)
                {
                    FileId = Ar.Read<uint>();
                    if (SourceType is not EAKBKSourceType.Streaming)
                        FileOffset = Ar.Read<uint>();

                    InMemoryMediaSize = Ar.Read<uint>();
                }
                else
                {
                    InMemoryMediaSize = Ar.Read<uint>();
                }
                break;
            default:
                CacheId = Ar.Read<uint>();
                InMemoryMediaSize = Ar.Read<uint>();
                break;
        }

        var sourceBits = Ar.Read<byte>();
        if (WwiseVersions.Version <= 112)
        {
            BankSourceFlags = ((EBankSourceFlags_v112) sourceBits).MapToCurrent();
        }
        else
        {
            BankSourceFlags = (EBankSourceFlags) sourceBits;
        }

        bool alwaysParam;
        switch (WwiseVersions.Version)
        {
            case <= 26:
                HasPluginParams = true;
                alwaysParam = true;
                break;
            case <= 126:
                HasPluginParams = PluginType is EAkPluginType.Source or EAkPluginType.MotionSource;
                alwaysParam = false;
                break;
            default:
                HasPluginParams = PluginType is EAkPluginType.Source;
                alwaysParam = false;
                break;
        }

        if (HasPluginParams)
            PluginParams = WwisePlugin.TryParsePluginParams(Ar, PluginId, alwaysParam);
    }

    public void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WritePropertyName(nameof(PluginId));
        writer.WriteValue(HasPluginParams ? PluginId.ToString() : PluginId); // We won't map to enum if it has no params

        writer.WritePropertyName(nameof(PluginType));
        writer.WriteValue(PluginType.ToString());

        writer.WritePropertyName(nameof(SourceType));
        writer.WriteValue(SourceType.ToString());

        if (DataIndex is not 0)
        {
            writer.WritePropertyName(nameof(DataIndex));
            writer.WriteValue(DataIndex);
        }

        if (SampleRate is not 0)
        {
            writer.WritePropertyName(nameof(SampleRate));
            writer.WriteValue(SampleRate);
        }

        if (FormatBits is not 0)
        {
            writer.WritePropertyName(nameof(FormatBits));
            writer.WriteValue(FormatBits);
        }

        writer.WritePropertyName(nameof(SourceId));
        writer.WriteValue(SourceId);

        if (FileId is not 0)
        {
            writer.WritePropertyName(nameof(FileId));
            writer.WriteValue(FileId);
        }

        if (CacheId is not 0)
        {
            writer.WritePropertyName(nameof(CacheId));
            writer.WriteValue(CacheId);
        }

        if (FileOffset is not 0)
        {
            writer.WritePropertyName(nameof(FileOffset));
            writer.WriteValue(FileOffset);
        }

        if (InMemoryMediaSize is not 0)
        {
            writer.WritePropertyName(nameof(InMemoryMediaSize));
            writer.WriteValue(InMemoryMediaSize);
        }

        writer.WritePropertyName(nameof(BankSourceFlags));
        writer.WriteValue(BankSourceFlags.ToString());

        if (PluginParams is not null)
        {
            writer.WritePropertyName(nameof(PluginParams));
            serializer.Serialize(writer, PluginParams);
        }
        else
        {
            writer.WritePropertyName(nameof(HasPluginParams));
            writer.WriteValue(HasPluginParams);
        }
    }
}

