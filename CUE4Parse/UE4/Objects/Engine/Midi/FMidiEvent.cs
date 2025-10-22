using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.Engine.Midi;

public class FMidiEvent : IUStruct
{
    public int Tick;
    public FMidiMsg Message;
    public FMidiEvent(FAssetArchive Ar)
    {
        Tick = Ar.Read<int>();
        Message = new FMidiMsg(Ar);
    }
}