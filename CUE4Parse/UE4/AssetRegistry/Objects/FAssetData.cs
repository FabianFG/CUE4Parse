using System;
using System.Collections.Generic;
using CUE4Parse.UE4.AssetRegistry.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.AssetRegistry.Objects
{
    [JsonConverter(typeof(FAssetDataConverter))]
    public class FAssetData
    {
        public readonly FName PackageName;
        public readonly FName PackagePath;
        public readonly FName AssetName;
        public readonly FName AssetClass;
        public IDictionary<FName, string> TagsAndValues;
        public FAssetBundleData TaggedAssetBundles;
        public readonly int[] ChunkIDs;
        public readonly uint PackageFlags;

        public FAssetData(FAssetRegistryArchive Ar)
        {
            if (Ar.Header.Version < FAssetRegistryVersionType.RemoveAssetPathFNames)
            {
                var oldObjectPath = Ar.ReadFName();
            }
            PackagePath = Ar.ReadFName();
            AssetClass = Ar.Header.Version >= FAssetRegistryVersionType.ClassPaths ? new FTopLevelAssetPath(Ar).AssetName : Ar.ReadFName();
            if (Ar.Header.Version < FAssetRegistryVersionType.RemovedMD5Hash)
            {
                var oldGroupNames = Ar.ReadFName();
            }
            PackageName = Ar.ReadFName();
            AssetName = Ar.ReadFName();

            Ar.SerializeTagsAndBundles(this);

            if (Ar.Ver >= EUnrealEngineObjectUE4Version.CHANGED_CHUNKID_TO_BE_AN_ARRAY_OF_CHUNKIDS)
            {
                ChunkIDs = Ar.ReadArray<int>();
            }
            else if (Ar.Ver >= EUnrealEngineObjectUE4Version.ADDED_CHUNKID_TO_ASSETDATA_AND_UPACKAGE)
            {
                ChunkIDs = new[] { Ar.Read<int>() };
            }

            if (Ar.Ver >= EUnrealEngineObjectUE4Version.COOKED_ASSETS_IN_EDITOR_SUPPORT)
            {
                PackageFlags = Ar.Read<uint>();
            }
        }

        public string ObjectPath => $"{PackageName}.{AssetName}";
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

            writer.WritePropertyName("AssetClass");
            serializer.Serialize(writer, value.AssetClass);

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