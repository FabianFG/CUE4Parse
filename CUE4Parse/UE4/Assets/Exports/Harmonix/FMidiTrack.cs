using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Assets.Exports.Harmonix;

[StructFallback]
public struct FMidiTrack
{
    public FMidiEvent[] Events;
    public string[] Strings;
    
    public FMidiTrack(FStructFallback fallback)
    {
        Events = fallback.GetOrDefault<FMidiEvent[]>(nameof(Events));
        Strings = fallback.GetOrDefault<string[]>(nameof(Strings));
    }

    public string GetTextAtIndex(int index) => Strings[index];
}