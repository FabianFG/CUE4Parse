using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Assets.Exports.Harmonix;

[StructFallback]
public struct FMidiFileData
{
    public FMidiTrack[] Tracks;
    public int TicksPerQuarterNote;
    
    public FMidiFileData(FStructFallback fallback)
    {
        Tracks = fallback.GetOrDefault<FMidiTrack[]>(nameof(Tracks), []);
        TicksPerQuarterNote = fallback.GetOrDefault(nameof(TicksPerQuarterNote), 960);
    }
}