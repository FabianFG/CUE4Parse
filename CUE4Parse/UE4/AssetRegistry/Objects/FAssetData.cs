using System;
using System.Collections.Generic;
using CUE4Parse.UE4.AssetRegistry.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.AssetRegistry.Objects
{
    [JsonConverter(typeof(FAssetDataConverter))]
    public class FAssetData
    {
        public readonly FName ObjectPath;
        public readonly FName PackageName;
        public readonly FName PackagePath;
        public readonly FName AssetName;
        public readonly FName AssetClass;
        public readonly FTopLevelAssetPath AssetClassPath;
        public IDictionary<FName, string> TagsAndValues;
        public FAssetBundleData TaggedAssetBundles;
        public readonly int[] ChunkIDs;
        public readonly uint PackageFlags;

        public FAssetData(FAssetRegistryArchive Ar, FAssetRegistryVersionType version)
        {
            ObjectPath = Ar.ReadFName();
            PackagePath = Ar.ReadFName();
            
            if (version < FAssetRegistryVersionType.ClassPaths)
            {
                AssetClass = Ar.ReadFName();
            }
            else
            {
                AssetClassPath = new FTopLevelAssetPath(Ar);
            }

            PackageName = Ar.ReadFName();
            AssetName = Ar.ReadFName();

            Ar.SerializeTagsAndBundles(this);

            ChunkIDs = Ar.ReadArray<int>();
            PackageFlags = Ar.Read<uint>();
        }
    }

    public class FAssetDataConverter : JsonConverter<FAssetData>
    {
        public override void WriteJson(JsonWriter writer, FAssetData value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("ObjectPath");
            serializer.Serialize(writer, value.ObjectPath);

            writer.WritePropertyName("PackageName");
            serializer.Serialize(writer, value.PackageName);

            writer.WritePropertyName("PackagePath");
            serializer.Serialize(writer, value.PackagePath);

            writer.WritePropertyName("AssetName");
            serializer.Serialize(writer, value.AssetName);

            if (value.AssetClass != default(FName))
            {
                serializer.Serialize(writer, value.AssetClass);
            }
            else
            {
                serializer.Serialize(writer, value.AssetClassPath);
            }

            if (value.TagsAndValues.Count > 0)
            {
                writer.WritePropertyName("TagsAndValues");
                serializer.Serialize(writer, value.TagsAndValues);
            }

            if (value.TaggedAssetBundles.Bundles.Length > 0)
            {
                writer.WritePropertyName("TaggedAssetBundles");
                serializer.Serialize(writer, value.TaggedAssetBundles);
            }

            if (value.ChunkIDs.Length > 0)
            {
                writer.WritePropertyName("ChunkIDs");
                serializer.Serialize(writer, value.ChunkIDs);
            }

            if (value.PackageFlags != 0)
            {
                writer.WritePropertyName("PackageFlags");
                serializer.Serialize(writer, value.PackageFlags);
            }

            writer.WriteEndObject();
        }

        public override FAssetData ReadJson(JsonReader reader, Type objectType, FAssetData existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}