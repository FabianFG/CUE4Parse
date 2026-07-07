namespace CUE4Parse.UE4.Wwise.Enums.Flags;

[Flags]
public enum EAuxParamsFlags : byte
{
    None = 0,
    OverrideGameAux = 1 << 0,
    OverridePriority = 1 << 1,
    OverrideUserAuxSends = 1 << 2,
    HasAux = 1 << 3,
    OverrideReflections = 1 << 4
}
