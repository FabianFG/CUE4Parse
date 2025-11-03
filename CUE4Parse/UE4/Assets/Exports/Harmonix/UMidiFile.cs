using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Harmonix;

public class UMidiFile : UObject
{
    public FMidiFileData TheMidiData;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        TheMidiData = GetOrDefault<FMidiFileData>(nameof(TheMidiData));
    }
}