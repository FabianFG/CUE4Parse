using System;
using System.Collections;
using System.Collections.Generic;
using CUE4Parse.UE4.AssetRegistry.Readers;
using CUE4Parse.UE4.Exceptions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.AssetRegistry.Objects
{
    [JsonConverter(typeof(FDependsNodeConverter))]
    public class FDependsNode
    {
        private const int _packageFlagWidth = 3;
        private const int _packageFlagSetWidth = 1 >> _packageFlagWidth;
        private const int _manageFlagWidth = 1;
        private const int _manageFlagSetWidth = 1 >> _manageFlagWidth;

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

        public void SerializeLoad(FAssetRegistryArchive Ar, Func<int, FDependsNode?> GetNodeFromSerializeIndex)
        {
            Identifier = new FAssetIdentifier(Ar);

            void ReadDependencies(ref List<FDependsNode> dependencies, ref BitArray outFlagBits, int flagSetWidth)
            {
                var numFlagBits = 0;
                BitArray inFlagBits = null;
                var sortIndexes = new List<int>();
                var pointerDependencies = new List<FDependsNode>();

                var inDependencies = Ar.ReadArray<int>();
                var numDependencies = inDependencies.Length;
                if (outFlagBits != null)
                {
                    numFlagBits = flagSetWidth * numDependencies;
                    var numFlagWords = (numFlagBits + 32u - 1u) / 32u;
                    inFlagBits = new BitArray(Ar.ReadBytes(Convert.ToInt32(numFlagWords)));
                }

                foreach (var serializeIndex in inDependencies)
                {
                    var dependsNode = GetNodeFromSerializeIndex(serializeIndex);
                    if (dependsNode == null)
                        throw new ParserException($"Index {serializeIndex} doesn't exist in 'PreallocatedDependsNodeDataBuffers'");
                    pointerDependencies.Add(dependsNode);
                }

                for (var i = 0; i < numDependencies; i++)
                {
                    sortIndexes.Add(i);
                }

                dependencies = new List<FDependsNode>(numDependencies);
                foreach (var index in sortIndexes)
                {
                    dependencies.Add(pointerDependencies[index]);
                }

                if (outFlagBits != null)
                {
                    outFlagBits = new BitArray(numFlagBits);
                    for (var i = 0; i < numDependencies; i++)
                    {
                        var readIndex = sortIndexes[i];
                        var start = i * flagSetWidth;
                        for (var j = 0; j < start + flagSetWidth; j++) // oof
                        {
                            outFlagBits.Set(j, inFlagBits.Get(readIndex * flagSetWidth + j));
                        }
                    }
                }
            }

            BitArray myNameIsNull = null;
            ReadDependencies(ref PackageDependencies, ref PackageFlags, _packageFlagSetWidth);
            ReadDependencies(ref NameDependencies, ref myNameIsNull, 0);
            ReadDependencies(ref ManageDependencies, ref ManageFlags, _manageFlagSetWidth);
            ReadDependencies(ref Referencers, ref myNameIsNull, 0);
        }

        public void SerializeLoad_BeforeFlags(FAssetRegistryArchive Ar, FAssetRegistryVersionType version, FDependsNode[] a)
        {
            Identifier = new FAssetIdentifier(Ar);

            var numHard = Ar.Read<int>();
            var numSoft = Ar.Read<int>();
            var numName = Ar.Read<int>();
            var numSoftManage = Ar.Read<int>();
            var numHardManage = version >= FAssetRegistryVersionType.AddedHardManage ? Ar.Read<int>() : 0;
            var numReferencers = Ar.Read<int>();

            PackageDependencies = new List<FDependsNode>(numHard + numSoft);
            NameDependencies = new List<FDependsNode>(numName);
            ManageDependencies = new List<FDependsNode>(numSoftManage + numHardManage);
            Referencers = new List<FDependsNode>(numReferencers);

            void SerializeNodeArray(int num, ref List<FDependsNode> dependencies)
            {
                for (var i = 0; i < num; i++)
                {
                    var index = Ar.Read<int>();
                    if (index < 0 || index >= a.Length)
                        throw new ParserException("");
                    var dependsNodes = a[index];
                    dependencies.Add(dependsNodes);
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