using CUE4Parse.GameTypes.WarnerBros.GothamKnights.Assets.Exports.Wwise;

namespace CUE4Parse.UE4.Wwise;

public partial class WwiseProvider
{
    private bool _gothamKnightsBanksCached;
    private const string SoundBanksPath = "Mercury/Content/Audio/SoundBanks/";

    public List<WwiseExtractedSound> ExtractGothamKnightsAudioEventSounds(UOrpheusEvent orpheusEvent)
    {
        CacheGothamKnightsSoundBanks();

        var results = new List<WwiseExtractedSound>();
        LoopThroughEvent(orpheusEvent.AudioEventId, results, GetOwnerDirectory(orpheusEvent), orpheusEvent.Name);

        return results;
    }

    private void CacheGothamKnightsSoundBanks()
    {
        if (_gothamKnightsBanksCached)
            return;

        _gothamKnightsBanksCached = true;
        foreach (var file in _provider.Files.Values)
        {
            if (!file.Path.StartsWith(SoundBanksPath, StringComparison.OrdinalIgnoreCase) || !file.Extension.Equals("uasset", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                var package = _provider.LoadPackage(file.Path);
                foreach (var bank in package.GetExports().OfType<UOrpheusBank>())
                {
                    if (bank.SoundBank is null)
                        continue;
                    if (bank.SoundBankId != 0 && !_wwiseLoadedSoundBanks.Add(bank.SoundBankId))
                        continue;

                    CacheWwiseFile(bank.SoundBank);
                }
            }
            catch (Exception e)
            {
                Log.Warning(e, "Failed to cache Gotham Knights soundbank '{FileName}'", file.Path);
            }
        }
    }
}
