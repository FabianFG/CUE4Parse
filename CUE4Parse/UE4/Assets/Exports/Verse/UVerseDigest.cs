using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Verse;

public enum EVerseDigestVariant : byte
{
    PublicAndEpicInternal = 0,
    PublicOnly = 1,
}

public enum EVerseDigestDataPayload : long
{
    None = 0,
    EditorOnly = 1,
    Normal = 2,
};

/// <summary>
/// Verse Visual Process Language
/// </summary>
public class UVerseDigest : UObject
{
    public string ProjectName { get; private set; }
    public EVerseDigestVariant Variant { get; private set; }
    public byte[]? DigestCode;
    public byte[]? ManifestCode;
    public FManifestDependency[]? DependencyPackages;
    public FSoftObjectPath[]? ClassImports;
    public FName[]? DigestDependencyPackageNames;
    public FPreloadAssetSet[]? PreloadableAssets;

    public byte[]? ReadableCode => DigestCode ?? GetOrDefault<byte[]>("DigestCode");

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        ProjectName = GetOrDefault<string>(nameof(ProjectName));
        Variant = GetOrDefault<EVerseDigestVariant>(nameof(Variant));
        if (Ar.Game >= GAME_UE6_0)
        {
            var payloadValue = Ar.Read<EVerseDigestDataPayload>();
            if (payloadValue is EVerseDigestDataPayload.None or EVerseDigestDataPayload.EditorOnly) return;
            LoadDigestPayload(Ar);
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(DependencyPackages));
        serializer.Serialize(writer, DependencyPackages);

        writer.WritePropertyName(nameof(ClassImports));
        serializer.Serialize(writer, ClassImports);

        writer.WritePropertyName(nameof(DigestDependencyPackageNames));
        serializer.Serialize(writer, DigestDependencyPackageNames);

        writer.WritePropertyName(nameof(PreloadableAssets));
        serializer.Serialize(writer, PreloadableAssets);
    }

    public void LoadDigestPayload(FAssetArchive Ar)
    {
        DigestCode = Ar.ReadArray<byte>();
        ManifestCode = Ar.ReadArray<byte>();
        DependencyPackages = Ar.ReadArray(() => new FManifestDependency(Ar));
        ClassImports = Ar.ReadArray(() => new FSoftObjectPath(Ar));
        DigestDependencyPackageNames = Ar.ReadArray(Ar.ReadFName);
        PreloadableAssets = Ar.ReadArray(() => new FPreloadAssetSet(Ar));
    }

    public struct FManifestDependency(FAssetArchive Ar)
    {
       public byte[] Dependency = Ar.ReadArray<byte>();
    };

    public struct FPreloadAssetSet(FAssetArchive Ar)
    {
        public string VersePath = Ar.ReadFString();
        public FPrimaryAssetId[] PreloadAssets = Ar.ReadArray(() => new FPrimaryAssetId(Ar));
    };
}
