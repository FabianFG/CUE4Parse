using System.IO;

namespace CUE4Parse.UE4.FMod.Objects;

public readonly struct FTriggerBoxParameterLayout
{
    public readonly FModGuid InstrumentGuid;
    public readonly float Start;
    public readonly float End;
    public readonly bool IncludeEnd;

    public FTriggerBoxParameterLayout(BinaryReader Ar)
    {
        InstrumentGuid = new FModGuid(Ar);
        Start = Ar.ReadSingle();
        End = Ar.ReadSingle();
        IncludeEnd = Ar.ReadBoolean();
    }

    public FTriggerBoxParameterLayout(FModGuid instrument, float start, float end, bool includeEnd)
    {
        InstrumentGuid = instrument;
        Start = start;
        End = end;
        IncludeEnd = includeEnd;
    }
}
