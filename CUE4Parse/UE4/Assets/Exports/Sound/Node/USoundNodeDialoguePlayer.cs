using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Sound.Node;

public class USoundNodeDialoguePlayer : USoundNode
{
    public FDialogueWaveParameter DialogueWaveParameter;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        DialogueWaveParameter = GetOrDefault<FDialogueWaveParameter>(nameof(DialogueWaveParameter));
    }
}