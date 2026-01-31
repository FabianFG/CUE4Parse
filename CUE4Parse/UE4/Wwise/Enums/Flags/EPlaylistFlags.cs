using System;

namespace CUE4Parse.UE4.Wwise.Enums.Flags;

[Flags]
public enum EPlayListFlags : byte
{
    None = 0,
    IsUsingWeight = 1 << 0,
    ResetPlayListAtEachPlay = 1 << 1,
    IsRestartBackward = 1 << 2,
    IsContinuous = 1 << 3,
    IsGlobal = 1 << 4
}
