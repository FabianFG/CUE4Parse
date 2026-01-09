using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Serilog;

namespace CUE4Parse.UE4.AssetRegistry;

public class FPartialAssetRegistryState
{
    public FPartialAssetData[] PreallocatedAssetDataBuffers = [];

    public FPartialAssetRegistryState() { }

    public FPartialAssetRegistryState(FArchive Ar)
    {
        var header = new FAssetRegistryHeader(Ar);
        switch (header.Version)
        {
            case < FAssetRegistryVersionType.AddAssetRegistryState:
                Log.Warning("Cannot read registry state before {Version}", header.Version);
                break;
            case < FAssetRegistryVersionType.FixedTags:
                {
                    var nameTableReader = new FNameTableArchiveReader(Ar, header);
                    PreallocatedAssetDataBuffers = nameTableReader.ReadArray(() => new FPartialAssetData(nameTableReader));
                    break;
                }
            default:
                {
                    var reader = new FAssetRegistryReader(Ar, header);
                    reader.IsFilterEditorOnly = header.bFilterEditorOnlyData;
                    PreallocatedAssetDataBuffers = Ar.ReadArray(() => new FPartialAssetData(reader));
                    break;
                }
        }
    }
}
