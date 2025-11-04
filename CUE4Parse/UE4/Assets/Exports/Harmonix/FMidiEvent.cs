using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Assets.Exports.Harmonix;

[StructFallback]
public struct FMidiEvent : IUStruct
{
    public int Tick;
    public FMidiMsg Message;
    
    public FMidiEvent(FAssetArchive Ar)
    {
        Tick = Ar.Read<int>();
        Message = new FMidiMsg(Ar);
    }

    public FMidiEvent(FStructFallback fallback)
    {
        Tick = fallback.GetOrDefault<int>(nameof(Tick));
        Message = fallback.GetOrDefault<FMidiMsg>(nameof(Message));
    }
}