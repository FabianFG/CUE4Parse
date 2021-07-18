using System;
using CUE4Parse.UE4.AssetRegistry.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Core.Serialization;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.AssetRegistry.Objects
{
    [JsonConverter(typeof(FAssetPackageDataConverter))]
    public class FAssetPackageData
    {
        public readonly FName PackageName;
        public readonly FGuid PackageGuid;
        public readonly FMD5Hash? CookedHash;
        public readonly FName[]? ImportedClasses;
        public readonly long DiskSize;
        public readonly int FileVersionUE = -1;
        public readonly int FileVersionLicenseeUE = -1;
        public readonly FCustomVersion[]? CustomVersions;
        public readonly uint Flags;

        public FAssetPackageData(FAssetRegistryArchive Ar, FAssetRegistryVersionType version)
        {
            PackageName = Ar.ReadFName();
            DiskSize = Ar.Read<long>();
            PackageGuid = Ar.Read<FGuid>();
            if (version >= FAssetRegistryVersionType.AddedCookedMD5Hash)
            {
                CookedHash = new FMD5Hash(Ar);
            }
            if (version >= FAssetRegistryVersionType.WorkspaceDomain)
            {
                FileVersionUE = Ar.Read<int>();
                FileVersionLicenseeUE = Ar.Read<int>();
                Flags = Ar.Read<uint>();
                CustomVersions = Ar.ReadArray<FCustomVersion>();
            }
            if (version >= FAssetRegistryVersionType.PackageImportedClasses)
            {
                ImportedClasses = Ar.ReadArray(Ar.ReadFName);
            }
        }
    }

    public class FAssetPackageDataConverter : JsonConverter<FAssetPackageData>
    {
        public override void WriteJson(JsonWriter writer, FAssetPackageData value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("PackageName");
            serializer.Serialize(writer, value.PackageName);

            writer.WritePropertyName("DiskSize");
            serializer.Serialize(writer, value.DiskSize);

            writer.WritePropertyName("PackageGuid");
            serializer.Serialize(writer, value.PackageGuid);

            if (value.CookedHash != null)
            {
                writer.WritePropertyName("CookedHash");
                serializer.Serialize(writer, value.CookedHash);
            }

            if (value.FileVersionUE != -1)
            {
                writer.WritePropertyName("FileVersionUE");
                serializer.Serialize(writer, value.FileVersionUE);
            }

            if (value.FileVersionLicenseeUE != -1)
            {
                writer.WritePropertyName("FileVersionLicenseeUE");
                serializer.Serialize(writer, value.FileVersionLicenseeUE);
            }

            if (value.Flags != 0)
            {
                writer.WritePropertyName("Flags");
                serializer.Serialize(writer, value.Flags);
            }

            if (value.CustomVersions is { Length: > 0 })
            {
                writer.WritePropertyName("CustomVersions");
                serializer.Serialize(writer, value.CustomVersions);
            }

            if (value.ImportedClasses is { Length: > 0 })
            {
                writer.WritePropertyName("ImportedClasses");
                serializer.Serialize(writer, value.ImportedClasses);
            }

            writer.WriteEndObject();
        }

        public override FAssetPackageData ReadJson(JsonReader reader, Type objectType, FAssetPackageData existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}