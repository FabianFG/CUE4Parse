using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.PCG;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.PCG;

public class UPCGMetadata : UObject
{
    public FPCGMetadataDomainID? ArchiveDefaultDomain;
    public Dictionary<FPCGMetadataDomainID, FPCGMetadataDomain> MetadataDomains = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (FPCGCustomVersion.Get(Ar) < FPCGCustomVersion.Type.MultiLevelMetadata)
        {
            MetadataDomains[new FPCGMetadataDomainID()] = new FPCGMetadataDomain(Ar);
        }
        else
        {
            ArchiveDefaultDomain = Ar.Read<FPCGMetadataDomainID>();
            var domainIDs = Ar.ReadArray<FPCGMetadataDomainID>();
            foreach (var domain in domainIDs)
            {
                var bIsValid = Ar.ReadBoolean();
                if (!domain.IsDefault() && bIsValid)
                {
                    MetadataDomains[domain] = new FPCGMetadataDomain(Ar);
                }
            }
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        if (ArchiveDefaultDomain.HasValue)
        {
            writer.WritePropertyName(nameof(ArchiveDefaultDomain));
            serializer.Serialize(writer, ArchiveDefaultDomain);
        }

        writer.WritePropertyName(nameof(MetadataDomains));
        serializer.Serialize(writer, MetadataDomains);
    }
}
