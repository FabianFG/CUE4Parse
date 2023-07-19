using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Structs.AssetRegistry.Readers;
using Newtonsoft.Json;
using Serilog;
using FNameTableArchiveReader = CUE4Parse.UE4.AssetRegistry.Objects.FNameTableArchiveReader;

namespace CUE4Parse.UE4.Structs.AssetRegistry;

public class FAssetRegistryState : UnrealBase
{
    public FAssetData[]? PreallocatedAssetDataBuffers;
    public FDependsNode[]? PreallocatedDependsNodeDataBuffers;
    public FAssetPackageData[]? PreallocatedPackageDataBuffers;

    public FAssetRegistryState(FArchive Ar)
    {
        var header = new FAssetRegistryHeader(Ar);
        var version = header.Version;
        switch (version)
        {
            case < FAssetRegistryVersionType.AddAssetRegistryState:
                Log.Warning("Cannot read registry state before {Version}", version);
                break;
            case < FAssetRegistryVersionType.FixedTags:
            {
                var nameTableReader = new FNameTableArchiveReader(Ar, header);
                Load(nameTableReader);
                break;
            }
            default:
            {
                var reader = new FAssetRegistryReader(Ar, header);
                Load(reader);
                break;
            }
        }
    }

    private void Load(FAssetRegistryArchive Ar)
    {
        PreallocatedAssetDataBuffers = Ar.ReadArray(() => new FAssetData(Ar));

        if (Ar.Header.Version < FAssetRegistryVersionType.RemovedMD5Hash)
            return; // Just ignore the rest of this for now.

        if (Ar.Header.Version < FAssetRegistryVersionType.AddedDependencyFlags)
        {
            var localNumDependsNodes = Ar.Read<int>();
            PreallocatedDependsNodeDataBuffers = new FDependsNode[localNumDependsNodes];
            for (var i = 0; i < localNumDependsNodes; i++)
            {
                PreallocatedDependsNodeDataBuffers[i] = new FDependsNode(i);
            }

            if (localNumDependsNodes > 0)
            {
                LoadDependencies_BeforeFlags(Ar);
            }
        }
        else
        {
            var dependencySectionSize = Ar.Read<long>();
            var dependencySectionEnd = Ar.Position + dependencySectionSize;
            var localNumDependsNodes = Ar.Read<int>();
            PreallocatedDependsNodeDataBuffers = new FDependsNode[localNumDependsNodes];
            for (var i = 0; i < localNumDependsNodes; i++)
            {
                PreallocatedDependsNodeDataBuffers[i] = new FDependsNode(i);
            }

            if (localNumDependsNodes > 0)
            {
                LoadDependencies(Ar);
            }

            Ar.Position = dependencySectionEnd;
        }

        PreallocatedPackageDataBuffers = Ar.ReadArray(() => new FAssetPackageData(Ar));
    }

    private void LoadDependencies_BeforeFlags(FAssetRegistryArchive Ar)
    {
        if (PreallocatedDependsNodeDataBuffers is not { } nodes) return;
        foreach (var dependsNode in nodes)
        {
            dependsNode.SerializeLoad_BeforeFlags(Ar, PreallocatedDependsNodeDataBuffers);
        }
    }

    private void LoadDependencies(FAssetRegistryArchive Ar)
    {
        if (PreallocatedDependsNodeDataBuffers is not { } nodes) return;
        foreach (var dependsNode in nodes)
        {
            dependsNode.SerializeLoad(Ar, PreallocatedDependsNodeDataBuffers);
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WritePropertyName("PreallocatedAssetDataBuffers");
        serializer.Serialize(writer, PreallocatedAssetDataBuffers);

        writer.WritePropertyName("PreallocatedDependsNodeDataBuffers");
        serializer.Serialize(writer, PreallocatedDependsNodeDataBuffers);

        writer.WritePropertyName("PreallocatedPackageDataBuffers");
        serializer.Serialize(writer, PreallocatedPackageDataBuffers);
    }
}
