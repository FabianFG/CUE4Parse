using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Wwise;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

[JsonConverter(typeof(FWwisePackagedFileConverter))]
public class FWwisePackagedFile : FStructFallback
{
    public EWwisePackagingStrategy PackagingStrategy;
    public FName PathName;
    public FName ModularGameplayName;
    public bool bStreaming;
    public int PrefetchSize;
    public int MemoryAlignment;
    public bool bDeviceMemory;
    public uint Hash;
    public WwiseReader? BulkData;

    public FWwisePackagedFile(FStructFallback fallback)
    {
        PackagingStrategy = fallback.GetOrDefault(nameof(PackagingStrategy), EWwisePackagingStrategy.Source);
        PathName = fallback.GetOrDefault<FName>(nameof(PathName));
        ModularGameplayName = fallback.GetOrDefault<FName>(nameof(ModularGameplayName));
        bStreaming = fallback.GetOrDefault<bool>(nameof(bStreaming));
        PrefetchSize = fallback.GetOrDefault<int>(nameof(PrefetchSize));
        MemoryAlignment = fallback.GetOrDefault<int>(nameof(MemoryAlignment));
        bDeviceMemory = fallback.GetOrDefault<bool>(nameof(bDeviceMemory));
        Hash = fallback.GetOrDefault<uint>(nameof(Hash));
    }

    public FWwisePackagedFile(FAssetArchive ar) : base(ar, "WwisePackagedFile")
    {
        PackagingStrategy = GetOrDefault(nameof(PackagingStrategy), EWwisePackagingStrategy.Source);
        PathName = GetOrDefault<FName>(nameof(PathName));
        ModularGameplayName = GetOrDefault<FName>(nameof(ModularGameplayName));
        bStreaming = GetOrDefault<bool>(nameof(bStreaming));
        PrefetchSize = GetOrDefault<int>(nameof(PrefetchSize));
        MemoryAlignment = GetOrDefault<int>(nameof(MemoryAlignment));
        bDeviceMemory = GetOrDefault<bool>(nameof(bDeviceMemory));
        Hash = GetOrDefault<uint>(nameof(Hash));
    }

    public static FWwisePackagedFile? CreatePackagedFile(FStructFallback fallback, string propertyName)
    {
        var pfFallback = fallback.GetOrDefault<FStructFallback>(propertyName);
        if (pfFallback == null)
            return null;
        return new FWwisePackagedFile(pfFallback);
    }

    public void SerializeBulkData(FAssetArchive Ar)
    {
        var name = PathName.IsNone ? Hash.ToString() : PathName.ToString();
        if (PackagingStrategy is EWwisePackagingStrategy.BulkData)
        {
            var bulkData = new FByteBulkData(Ar);
            if (bulkData.Data is null) return;

            if (!bulkData.Header.BulkDataFlags.HasFlag(EBulkDataFlags.BULKDATA_PayloadInSeperateFile))
                Ar.Position += bulkData.Data.Length;

            try
            {
                using var reader = new FByteArchive("AkAssetData", bulkData.Data, Ar.Versions);
                BulkData = new WwiseReader(reader);
            }
            // i know it's ugly, but i don't see other solution without rewriting everything
            catch (RIFFSectionSizeException e)
            {
                if (bulkData.TryCombineBulkData(Ar, out var combinedData))
                {
                    try
                    {
                        using var reader = new FByteArchive("AkAssetData", combinedData, Ar.Versions);
                        BulkData = new WwiseReader(reader);
                    }
                    catch
                    {
                        Log.Error("Failed to read Wwise bank data for {Name} from combined bulk data", name);
                    }
                }
            }
            catch
            {
                Log.Error("Failed to read Wwise bank data for {Name} from bulk data", name);
            }
        }
        else if (PackagingStrategy is EWwisePackagingStrategy.External)
        {
        }
        else
        {
            Log.Warning("Wwise bank data for {Name} uses unsupported packaging strategy {stategy}", name,
                PackagingStrategy.ToString());
        }
    }
}
