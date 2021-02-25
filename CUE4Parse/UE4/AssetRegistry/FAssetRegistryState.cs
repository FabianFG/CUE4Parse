using System;
using System.IO;
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
        
        public FAssetRegistryState(FArchive Ar)
        {
            FAssetRegistryVersion.TrySerializeVersion(Ar, out var version);
            switch (version)
            {
                case < FAssetRegistryVersionType.RemovedMD5Hash:
                    Log.Warning($"Cannot read registry state before '{version}'");
                    break;
                case < FAssetRegistryVersionType.FixedTags:
                {
                    var nameTableReader = new FNameTableArchiveReader(Ar);
                    Load(nameTableReader, version);
                    break;
                }
                default:
                {
                    var reader = new FAssetRegistryReader(Ar);
                    Load(reader, version);
                    break;
                }
            }
        }

        private void Load(FAssetRegistryArchive Ar, FAssetRegistryVersionType version)
        {
            PreallocatedAssetDataBuffers = Ar.ReadArray(() => new FAssetData(Ar));
            
            if (version < FAssetRegistryVersionType.AddedDependencyFlags)
            {
                var localNumDependsNodes = Ar.Read<int>();
                PreallocatedDependsNodeDataBuffers = Ar.ReadArray(localNumDependsNodes, () => new FDependsNode());
                if (localNumDependsNodes > 0)
                {
                    LoadDependencies_BeforeFlags(Ar, version);
                }
            }
            else
            {
                var dependencySectionSize = Ar.Read<long>();
                var dependencySectionEnd = Ar.Position + dependencySectionSize;
                var localNumDependsNodes = Ar.Read<int>();
                PreallocatedDependsNodeDataBuffers = Ar.ReadArray(localNumDependsNodes, () => new FDependsNode());
                if (localNumDependsNodes > 0)
                {
                    LoadDependencies(Ar);
                }
                Ar.Seek(dependencySectionEnd, SeekOrigin.Begin);
            }

            var serializeHash = version < FAssetRegistryVersionType.AddedCookedMD5Hash;
            PreallocatedPackageDataBuffers = Ar.ReadArray(() => new FAssetPackageData(Ar, serializeHash));
        }

        private void LoadDependencies_BeforeFlags(FAssetRegistryArchive Ar, FAssetRegistryVersionType version)
        {
            foreach (var dependsNode in PreallocatedDependsNodeDataBuffers)
            {
                dependsNode.SerializeLoad_BeforeFlags(Ar, version, PreallocatedDependsNodeDataBuffers);
            }
        }

        private void LoadDependencies(FAssetRegistryArchive Ar)
        {
            FDependsNode? GetNodeFromSerializeIndex(int index)
            {
                if (index < 0 || PreallocatedDependsNodeDataBuffers.Length <= index)
                    return null;
                return PreallocatedDependsNodeDataBuffers[index];
            }
            
            foreach (var dependsNode in PreallocatedDependsNodeDataBuffers)
            {
                dependsNode.SerializeLoad(Ar, GetNodeFromSerializeIndex);
            }
        }
    }
    
    public class FAssetRegistryStateConverter : JsonConverter<FAssetRegistryState>
    {
        public override void WriteJson(JsonWriter writer, FAssetRegistryState value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            
            writer.WritePropertyName("PreallocatedAssetDataBuffers");
            serializer.Serialize(writer, value.PreallocatedAssetDataBuffers);
            
            writer.WritePropertyName("PreallocatedDependsNodeDataBuffers");
            serializer.Serialize(writer, value.PreallocatedDependsNodeDataBuffers);
            
            writer.WritePropertyName("PreallocatedPackageDataBuffers");
            serializer.Serialize(writer, value.PreallocatedPackageDataBuffers);

            writer.WriteEndObject();
        }

        public override FAssetRegistryState ReadJson(JsonReader reader, Type objectType, FAssetRegistryState existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}