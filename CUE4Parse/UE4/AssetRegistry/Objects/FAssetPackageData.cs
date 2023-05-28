using System;
using CUE4Parse.UE4.AssetRegistry.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Core.Serialization;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
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
        public readonly FPackageFileVersion FileVersionUE;
        public readonly int FileVersionLicenseeUE = -1;
        public readonly FCustomVersionContainer CustomVersions;
        public readonly uint Flags;

        public FAssetPackageData(FAssetRegistryArchive Ar)
        {
            PackageName = Ar.ReadFName();
            DiskSize = Ar.Read<long>();
            PackageGuid = Ar.Read<FGuid>();
            if (Ar.Header.Version >= FAssetRegistryVersionType.AddedCookedMD5Hash)
            {
                CookedHash = new FMD5Hash(Ar);
            }
            if (Ar.Header.Version >= FAssetRegistryVersionType.AddedChunkHashes)
            {
                // TMap<FIoChunkId, FIoHash> ChunkHashes;
                Ar.Position += Ar.Read<int>() * (12 + 20);
            }
            if (Ar.Header.Version >= FAssetRegistryVersionType.WorkspaceDomain)
            {
                if (Ar.Header.Version >= FAssetRegistryVersionType.PackageFileSummaryVersionChange)
                {
                    FileVersionUE = Ar.Read<FPackageFileVersion>();
                }
                else
                {
                    var ue4Version = Ar.Read<int>();
                    FileVersionUE = FPackageFileVersion.CreateUE4Version(ue4Version);
                }

                FileVersionLicenseeUE = Ar.Read<int>();
                Flags = Ar.Read<uint>();
                CustomVersions = new FCustomVersionContainer(Ar);
            }
            if (Ar.Header.Version >= FAssetRegistryVersionType.PackageImportedClasses)
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

            if (value.FileVersionUE.FileVersionUE4 != 0 || value.FileVersionUE.FileVersionUE5 != 0)
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

            if (value.CustomVersions.Versions is { Length: > 0 })
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
