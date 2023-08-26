using System;
using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.AssetRegistry.Readers;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.AssetRegistry
{
    [JsonConverter(typeof(FAssetRegistryStateConverter))]
    public class FAssetRegistryState
    {
        public FAssetData[] PreallocatedAssetDataBuffers;
        public FDependsNode[] PreallocatedDependsNodeDataBuffers;
        public FAssetPackageData[] PreallocatedPackageDataBuffers;

        public FAssetRegistryState()
        {
            PreallocatedAssetDataBuffers = Array.Empty<FAssetData>();
            PreallocatedDependsNodeDataBuffers = Array.Empty<FDependsNode>();
            PreallocatedPackageDataBuffers = Array.Empty<FAssetPackageData>();
        }

        public FAssetRegistryState(FArchive Ar) : this()
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
            foreach (var dependsNode in PreallocatedDependsNodeDataBuffers)
            {
                dependsNode.SerializeLoad_BeforeFlags(Ar, PreallocatedDependsNodeDataBuffers);
            }
        }

        private void LoadDependencies(FAssetRegistryArchive Ar)
        {
            foreach (var dependsNode in PreallocatedDependsNodeDataBuffers)
            {
                dependsNode.SerializeLoad(Ar, PreallocatedDependsNodeDataBuffers);
            }
        }
    }
}
