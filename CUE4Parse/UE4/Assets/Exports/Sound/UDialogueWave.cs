using CUE4Parse.UE4.Assets.Exports.Sound.Node;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Sound;

public class UDialogueWave : UObject
{
    public string SpokenText;
    public FDialogueContextMapping[] ContextMappings;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        SpokenText = GetOrDefault<string>(nameof(SpokenText));
        ContextMappings = GetOrDefault<FDialogueContextMapping[]>(nameof(ContextMappings), []);

        _ = Ar.ReadBoolean();
    }

    public FPackageIndex GetWaveFromContext(FDialogueContext context)
    {
        foreach (var contextMapping in ContextMappings)
        {
            if (contextMapping.Context == context)
                return contextMapping.SoundWave;
        }

        return new FPackageIndex();
    }
}