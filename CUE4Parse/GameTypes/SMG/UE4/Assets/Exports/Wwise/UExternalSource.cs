using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise;

namespace CUE4Parse.GameTypes.SMG.UE4.Assets.Exports.Wwise;

public class UExternalSource : UObject
{
    public string? ExternalSourcePath;
    public WwiseReader? Data;
    public float Duration;
    public string? FileHashString;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        ExternalSourcePath = GetOrDefault<string>(nameof(ExternalSourcePath));
        if (GetOrDefault<byte[]>(nameof(Data)) is { } data)
        {
            using var byteAr = new FByteArchive(ExternalSourcePath, data, Ar.Versions);
            Data = new WwiseReader(byteAr);
        }
        FileHashString = GetOrDefault<string>(nameof(FileHashString));
    }
}

