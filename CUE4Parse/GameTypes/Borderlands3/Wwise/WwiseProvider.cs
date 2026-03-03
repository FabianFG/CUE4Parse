using System.Collections.Generic;
using System.IO;
using CUE4Parse.GameTypes.Borderlands3.Assets.Exports;

namespace CUE4Parse.UE4.Wwise;

public partial class WwiseProvider
{
    // Technically UDialogEvent should be used and linked via FGuid to the dialog data
    // but we would need to add unnecessary handler for that, just extract from this data instead
    public List<WwiseExtractedSound> ExtractDialogBorderlands3(UDialogPerformanceData dialogPerfData)
    {
        DetermineBaseWwiseAudioPath();

        if (_looseWemFilesLookup.TryGetValue(dialogPerfData.WwiseEventShortID, out var location))
            TryLoadAndCacheWwiseFile(location.Path, Path.GetFileNameWithoutExtension(location.Path), 0, out var _, loadFromFileSystem: !location.InProvider, isWemFile: true);

        var wemId = dialogPerfData.WwiseEventShortID.ToString();
        var fileName = dialogPerfData.WwiseExternalMediaTemplate is null ? wemId : $"{dialogPerfData.WwiseExternalMediaTemplate.Name} ({wemId})";
        var results = new List<WwiseExtractedSound>();
        if (_wwiseEncodedMedia.TryGetValue(wemId, out var wemData))
        {
            var outputPath = Path.Combine(_baseWwiseAudioPath, fileName);
            if (outputPath.StartsWith('/'))
                outputPath = outputPath[1..];

            results.Add(new WwiseExtractedSound
            {
                OutputPath = outputPath.Replace('\\', '/'),
                Extension = "wem",
                Data = wemData,
            });
        }

        return results;
    }
}
