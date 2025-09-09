﻿using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

[StructFallback]
public readonly struct FWwiseSoundBankCookedData
{
    public readonly int SoundBankId;
	public readonly FName SoundBankPathName;
	public readonly int MemoryAlignment;
	public readonly bool bDeviceMemory;
	public readonly bool bContainsMedia;
	public readonly EWwiseSoundBankType SoundBankType;
	public readonly FName DebugName;

    public FWwiseSoundBankCookedData(FStructFallback fallback)
    {
        SoundBankId = fallback.GetOrDefault<int>(nameof(SoundBankId));
        SoundBankPathName = fallback.GetOrDefault<FName>(nameof(SoundBankPathName));
        MemoryAlignment = fallback.GetOrDefault<int>(nameof(MemoryAlignment));
        bDeviceMemory = fallback.GetOrDefault<bool>(nameof(bDeviceMemory));
        bContainsMedia = fallback.GetOrDefault<bool>(nameof(bContainsMedia));
        SoundBankType = fallback.GetOrDefault<EWwiseSoundBankType>(nameof(SoundBankType));
        DebugName = fallback.GetOrDefault<FName>(nameof(DebugName));
    }
}
