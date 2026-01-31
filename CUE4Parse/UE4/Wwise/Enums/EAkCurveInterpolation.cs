namespace CUE4Parse.UE4.Wwise.Enums;

public enum EAkCurveInterpolation : byte
{
    Log3 = 0x0,
    Sine = 0x1,
    Log1 = 0x2,
    InvSCurve = 0x3,
    Linear = 0x4,
    SCurve = 0x5,
    Exp1 = 0x6,
    SineRecip = 0x7,
    Exp3 = 0x8,
#pragma warning disable CA1069
    LastFadeCurve = 0x8,
#pragma warning restore CA1069
    Constant = 0x9
}
