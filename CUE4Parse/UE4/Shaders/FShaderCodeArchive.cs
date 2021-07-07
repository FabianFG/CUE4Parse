using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Shaders
{
    [JsonConverter(typeof(FShaderCodeArchiveConverter))]
    public class FShaderCodeArchive
    {
        public readonly List<byte[]> ShaderCode;
        public readonly FSerializedShaderArchive SerializedShaders;
        public readonly Dictionary<FSHAHash, FShaderCodeEntry> PrevCookedShaders;

        public FShaderCodeArchive(FArchive Ar)
        {
            var archiveVersion = Ar.Read<uint>();

            if (archiveVersion == 2)
            {
                SerializedShaders = new FSerializedShaderArchive(Ar);

                ShaderCode = new List<byte[]>(SerializedShaders.ShaderEntries.Length);
                foreach (var entry in SerializedShaders.ShaderEntries)
                {
                    ShaderCode.Add(Ar.ReadBytes((int) entry.Size));
                }
            }
            else if (archiveVersion == 1)
            {
                // TODO - Need to figure out how this should work
                // https://github.com/EpicGames/UnrealEngine/blob/4.22/Engine/Source/Runtime/RenderCore/Private/ShaderCodeLibrary.cpp#L910
                
                // var mapVarNameNum = Ar.Read<int>();
                //
                // PrevCookedShaders = new Dictionary<FSHAHash, FShaderCodeEntry>(mapVarNameNum);
                // for (var i = 0; i < mapVarNameNum; ++i)
                // {
                //     PrevCookedShaders[Ar.Read<FSHAHash>()] = new FShaderCodeEntry(Ar);
                // }
            }
        }
    }

    public class FShaderCodeArchiveConverter : JsonConverter<FShaderCodeArchive>
    {
        public override void WriteJson(JsonWriter writer, FShaderCodeArchive value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("SerializedShaders");
            serializer.Serialize(writer, value.SerializedShaders);

            // TODO: Try to read this as actual data.
            // writer.WritePropertyName("ShaderCode");
            // serializer.Serialize(writer, value.ShaderCode);

            writer.WriteEndObject();
        }

        public override FShaderCodeArchive ReadJson(JsonReader reader, Type objectType, FShaderCodeArchive existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}