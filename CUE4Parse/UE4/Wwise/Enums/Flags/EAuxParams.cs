using System;

namespace CUE4Parse.UE4.Wwise.Enums.Flags;

[Flags]
public enum EAuxParams : byte
{
    None = 0,
    OverrideUserAuxSends = 1 << 2,
    HasAux = 1 << 3,
    OverrideReflections = 1 << 4
}
