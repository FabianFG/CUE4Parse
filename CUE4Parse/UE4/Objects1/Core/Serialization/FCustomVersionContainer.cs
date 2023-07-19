using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Core.Serialization;

[JsonConverter(typeof(FCustomVersionContainerConverter))]
public class FCustomVersionContainer
{
    public readonly FCustomVersion[] Versions;

    public FCustomVersionContainer()
    {
        Versions = Array.Empty<FCustomVersion>();
    }

    public FCustomVersionContainer(IEnumerable<FCustomVersion>? versions)
    {
        Versions = (versions ?? Array.Empty<FCustomVersion>()) .ToArray();
    }

    public FCustomVersionContainer(FArchive Ar, ECustomVersionSerializationFormat format = ECustomVersionSerializationFormat.Latest) : this()
    {
        switch (format)
        {
            case ECustomVersionSerializationFormat.Enums:
            {
                var oldTags = Ar.ReadArray<FEnumCustomVersion_DEPRECATED>();

                Versions = new FCustomVersion[oldTags.Length];
                for (var i = 0; i < Versions.Length; ++i)
                {
                    Versions[i] = oldTags[i].ToCustomVersion();
                }

                break;
            }
            case ECustomVersionSerializationFormat.Guids:
            {
                var versionArray = Ar.ReadArray(() => new FGuidCustomVersion_DEPRECATED(Ar));

                Versions = new FCustomVersion[versionArray.Length];
                for (var i = 0; i < Versions.Length; ++i)
                {
                    Versions[i] = versionArray[i].ToCustomVersion();
                }

                break;
            }
            case ECustomVersionSerializationFormat.Optimized:
            {
                Versions = Ar.ReadArray<FCustomVersion>();
                break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetVersion(FGuid customKey)
    {
        for (var i = 0; i < Versions.Length; i++)
        {
            if (Versions[i].Key == customKey)
            {
                return Versions[i].Version;
            }
        }

        return -1;
    }
}

public class FCustomVersionContainerConverter : JsonConverter<FCustomVersionContainer>
{
    public override void WriteJson(JsonWriter writer, FCustomVersionContainer? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value?.Versions);
    }

    public override FCustomVersionContainer ReadJson(JsonReader reader, Type objectType, FCustomVersionContainer? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
