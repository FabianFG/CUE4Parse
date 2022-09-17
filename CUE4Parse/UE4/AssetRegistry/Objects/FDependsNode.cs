using System;
using System.Collections;
using System.Collections.Generic;
using CUE4Parse.UE4.AssetRegistry.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.AssetRegistry.Objects
{
    [JsonConverter(typeof(FDependsNodeConverter))]
    public class FDependsNode
    {
        private const int PackageFlagWidth = 3;
        private const int PackageFlagSetWidth = 5; // FPropertyCombinationPack3::StorageBitCount
        private const int ManageFlagWidth = 1;
        private const int ManageFlagSetWidth = 1; // TPropertyCombinationSet<1>::StorageBitCount

        public FAssetIdentifier Identifier;
        public List<FDependsNode> PackageDependencies;
        public List<FDependsNode> NameDependencies;
        public List<FDependsNode> ManageDependencies;
        public List<FDependsNode> Referencers;
        public BitArray? PackageFlags;
        public BitArray? ManageFlags;

        internal int _index;

        public FDependsNode(int index)
        {
            _index = index;
        }

        public void SerializeLoad(FAssetRegistryArchive Ar, FDependsNode[] preallocatedDependsNodeDataBuffer)
        {
            Identifier = new FAssetIdentifier(Ar);

            void ReadDependencies(ref List<FDependsNode> outDependencies, ref BitArray? outFlagBits, int flagSetWidth)
            {
                var sortIndexes = new List<int>();
                var pointerDependencies = new List<FDependsNode>();

                var inDependencies = Ar.ReadArray<int>();
                var numDependencies = inDependencies.Length;
                var numFlagBits = flagSetWidth * numDependencies;
                var numFlagWords = numFlagBits.DivideAndRoundUp(32);
                var inFlagBits = numFlagWords != 0 ? new BitArray(Ar.ReadArray<int>(numFlagWords)) : new BitArray(0);

                foreach (var serializeIndex in inDependencies)
                {
                    if (serializeIndex < 0 || preallocatedDependsNodeDataBuffer.Length <= serializeIndex)
                        throw new ParserException($"Index {serializeIndex} doesn't exist in 'PreallocatedDependsNodeDataBuffers'");
                    var dependsNode = preallocatedDependsNodeDataBuffer[serializeIndex];
                    pointerDependencies.Add(dependsNode);
                }

                for (var i = 0; i < numDependencies; i++)
                {
                    sortIndexes.Add(i);
                }

                sortIndexes.Sort((a, b) => pointerDependencies[a]._index - pointerDependencies[b]._index);

                outDependencies = new List<FDependsNode>(numDependencies);
                foreach (var index in sortIndexes)
                {
                    outDependencies.Add(pointerDependencies[index]);
                }

                outFlagBits = new BitArray(numFlagBits);
                for (var writeIndex = 0; writeIndex < numDependencies; writeIndex++)
                {
                    var readIndex = sortIndexes[writeIndex];
                    outFlagBits.SetRangeFromRange(writeIndex * flagSetWidth, flagSetWidth, inFlagBits, readIndex * flagSetWidth);
                }
            }

            void ReadDependenciesNoFlags(ref List<FDependsNode> outDependencies)
            {
                var sortIndexes = new List<int>();
                var pointerDependencies = new List<FDependsNode>();

                var inDependencies = Ar.ReadArray<int>();
                var numDependencies = inDependencies.Length;

                foreach (var serializeIndex in inDependencies)
                {
                    if (serializeIndex < 0 || preallocatedDependsNodeDataBuffer.Length <= serializeIndex)
                        throw new ParserException($"Index {serializeIndex} doesn't exist in 'PreallocatedDependsNodeDataBuffers'");
                    var dependsNode = preallocatedDependsNodeDataBuffer[serializeIndex];
                    pointerDependencies.Add(dependsNode);
                }

                for (var i = 0; i < numDependencies; i++)
                {
                    sortIndexes.Add(i);
                }

                sortIndexes.Sort((a, b) => pointerDependencies[a]._index - pointerDependencies[b]._index);

                outDependencies = new List<FDependsNode>(numDependencies);
                foreach (var index in sortIndexes)
                {
                    outDependencies.Add(pointerDependencies[index]);
                }
            }

            ReadDependencies(ref PackageDependencies, ref PackageFlags, PackageFlagSetWidth);
            ReadDependenciesNoFlags(ref NameDependencies);
            ReadDependencies(ref ManageDependencies, ref ManageFlags, ManageFlagSetWidth);
            ReadDependenciesNoFlags(ref Referencers);
        }

        public void SerializeLoad_BeforeFlags(FAssetRegistryArchive Ar, FDependsNode[] preallocatedDependsNodeDataBuffer)
        {
            Identifier = new FAssetIdentifier(Ar);

            var numHard = Ar.Read<int>();
            var numSoft = Ar.Read<int>();
            var numName = Ar.Read<int>();
            var numSoftManage = Ar.Read<int>();
            var numHardManage = Ar.Header.Version >= FAssetRegistryVersionType.AddedHardManage ? Ar.Read<int>() : 0;
            var numReferencers = Ar.Read<int>();

            PackageDependencies = new List<FDependsNode>(numHard + numSoft);
            NameDependencies = new List<FDependsNode>(numName);
            ManageDependencies = new List<FDependsNode>(numSoftManage + numHardManage);
            Referencers = new List<FDependsNode>(numReferencers);

            void SerializeNodeArray(int num, ref List<FDependsNode> outNodes)
            {
                for (var dependencyIndex = 0; dependencyIndex < num; ++dependencyIndex)
                {
                    var index = Ar.Read<int>();
                    if (index < 0 || index >= preallocatedDependsNodeDataBuffer.Length)
                        throw new ParserException($"Index {index} doesn't exist in 'PreallocatedDependsNodeDataBuffers'");
                    var dependsNode = preallocatedDependsNodeDataBuffer[index];
                    outNodes.Add(dependsNode);
                }
            }

            SerializeNodeArray(numHard, ref PackageDependencies);
            SerializeNodeArray(numSoft, ref PackageDependencies);
            SerializeNodeArray(numName, ref NameDependencies);
            SerializeNodeArray(numSoftManage, ref ManageDependencies);
            SerializeNodeArray(numHardManage, ref ManageDependencies);
            SerializeNodeArray(numReferencers, ref Referencers);
        }
    }

    public class FDependsNodeConverter : JsonConverter<FDependsNode>
    {
        public override void WriteJson(JsonWriter writer, FDependsNode value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("Identifier");
            serializer.Serialize(writer, value.Identifier);

            WriteDependsNodeList("PackageDependencies", writer, value.PackageDependencies);
            WriteDependsNodeList("NameDependencies", writer, value.NameDependencies);
            WriteDependsNodeList("ManageDependencies", writer, value.ManageDependencies);
            WriteDependsNodeList("Referencers", writer, value.Referencers);

            if (value.PackageFlags != null)
            {
                writer.WritePropertyName("PackageFlags");
                serializer.Serialize(writer, value.PackageFlags);
            }

            if (value.ManageFlags != null)
            {
                writer.WritePropertyName("ManageFlags");
                serializer.Serialize(writer, value.ManageFlags);
            }

            writer.WriteEndObject();
        }

        /** Custom serializer to avoid circular reference */
        private static void WriteDependsNodeList(string name, JsonWriter writer, List<FDependsNode> dependsNodeList)
        {
            if (dependsNodeList.Count == 0)
            {
                return;
            }

            writer.WritePropertyName(name);
            writer.WriteStartArray();
            foreach (var dependsNode in dependsNodeList)
            {
                writer.WriteValue(dependsNode._index);
            }
            writer.WriteEndArray();
        }

        public override FDependsNode ReadJson(JsonReader reader, Type objectType, FDependsNode existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}