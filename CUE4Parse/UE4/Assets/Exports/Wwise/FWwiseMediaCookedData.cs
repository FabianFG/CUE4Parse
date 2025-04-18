using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

[StructFallback]
public readonly struct FWwiseMediaCookedData
{
    public readonly int MediaId;
    public readonly FName MediaPathName;
    public readonly int PrefetchSize;
    public readonly int MemoryAlignment;
    public readonly bool bDeviceMemory;
    public readonly bool bStreaming;
    public readonly FName DebugName;

    public FWwiseMediaCookedData(FStructFallback fallback)
    {
        MediaId = fallback.GetOrDefault<int>(nameof(MediaId));
        MediaPathName = fallback.GetOrDefault<FName>(nameof(MediaPathName));
        PrefetchSize = fallback.GetOrDefault<int>(nameof(PrefetchSize));
        MemoryAlignment = fallback.GetOrDefault<int>(nameof(MemoryAlignment));
        bDeviceMemory = fallback.GetOrDefault<bool>(nameof(bDeviceMemory));
        bStreaming = fallback.GetOrDefault<bool>(nameof(bStreaming));
        DebugName = fallback.GetOrDefault<FName>(nameof(DebugName));
    }
}
