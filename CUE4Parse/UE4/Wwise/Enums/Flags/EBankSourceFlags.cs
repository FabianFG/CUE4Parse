using System;

namespace CUE4Parse.UE4.Wwise.Enums.Flags;

// > 112
[Flags]
public enum EBankSourceFlags : byte
{
    None = 0,
    IsLanguageSpecific = 1 << 0,
    Prefetch = 1 << 1,
    ExternallySupplied = 1 << 2, // Legacy, check below
    NonCachable = 1 << 3,
    HasSource = 1 << 7
}

// <= 112
[Flags]
public enum EBankSourceFlags_v112 : byte
{
    None = 0,
    IsLanguageSpecific = 1 << 0,
    HasSource = 1 << 1,
    ExternallySupplied = 1 << 2
}

public static class BankSourceFlagsExtensions
{
    public static EBankSourceFlags MapToCurrent(this EBankSourceFlags_v112 legacy)
    {
        var current = EBankSourceFlags.None;

        if (legacy.HasFlag(EBankSourceFlags_v112.IsLanguageSpecific))
            current |= EBankSourceFlags.IsLanguageSpecific;
        if (legacy.HasFlag(EBankSourceFlags_v112.HasSource))
            current |= EBankSourceFlags.HasSource;
        if (legacy.HasFlag(EBankSourceFlags_v112.ExternallySupplied))
            current |= EBankSourceFlags.ExternallySupplied;

        return current;
    }
}
