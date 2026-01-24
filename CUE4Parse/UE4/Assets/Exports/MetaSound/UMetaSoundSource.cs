using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.MetaSound;

public class UMetaSoundSource : USoundWaveProcedural
{
    public FStructFallback Settings;
    public FMetasoundFrontendDocument RootMetasoundDocument;
    public string[] ReferencedAssetClassKeys;
    public FPackageIndex[] ReferencedAssetClassObjects;
    public EMetaSoundOutputAudioFormat OutputFormat;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        Settings = new FStructFallback(Ar, "MetaSoundQualitySettings");

        RootMetasoundDocument = GetOrDefault<FMetasoundFrontendDocument>(nameof(RootMetasoundDocument));
        ReferencedAssetClassKeys = GetOrDefault<string[]>(nameof(ReferencedAssetClassKeys), []);
        ReferencedAssetClassObjects = GetOrDefault<FPackageIndex[]>(nameof(ReferencedAssetClassObjects));
        OutputFormat = GetOrDefault<EMetaSoundOutputAudioFormat>(nameof(OutputFormat));
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        
        writer.WritePropertyName(nameof(Settings));
        serializer.Serialize(writer, Settings);
    }
}

public enum EMetaSoundOutputAudioFormat : byte
{
    Mono,
    Stereo,
    Quad,
    FiveDotOne,
    SevenDotOne,

    COUNT,
}