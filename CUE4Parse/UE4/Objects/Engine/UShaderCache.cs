using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Engine
{
    public struct FIndividualCompressedShaderInfo
    {
        public ushort ChunkIndex;
        public int UncompressedCodeOffset;
        public ushort UncompressedCodeLength;
    }

    public class FCompressedShaderCodeChunk(FArchive Ar)
    {
        public int UncompressedSize = Ar.Read<int>();
        public byte[] CompressedCode = Ar.ReadArray<byte>();
    }

    public class FTypeSpecificCompressedShaderCode(FArchive Ar)
    {
        public Dictionary<FGuid, FIndividualCompressedShaderInfo> CompressedShaderInfos = Ar.ReadMap(Ar.Read<FGuid>, Ar.Read<FIndividualCompressedShaderInfo>);
        public FCompressedShaderCodeChunk[] CodeChunks = Ar.ReadArray(() => new FCompressedShaderCodeChunk(Ar));
    }

    /*
    public struct FShader
    {
        public enum ShaderFrequency : byte
        {
            Vertex = 0,
            Pixel = 1,
            PixelUDK = 3
        }

        public FName ShaderType;
        public FGuid Guid;
        public ShaderFrequency Frequency;
        public byte[] ShaderByteCode;
        public uint ParameterMapCRC;
        public int InstructionCount;
        public byte Platform;
        public FName? VertexFactoryType;

        public FShader(FAssetArchive Ar)
        {
            if (Ar.Ver >= EUnrealEngineObjectUE3Version.FIXED_AUTO_SHADER_VERSIONING)
            {
                Ar.ReadArray<short>();
            }

            Platform = Ar.Read<byte>();
            Frequency = (ShaderFrequency)Ar.Read<byte>();
            ShaderByteCode = Ar.ReadArray<byte>();
            ParameterMapCRC = Ar.Read<uint>();
            Guid = Ar.Read<FGuid>();
            ShaderType = Ar.ReadFName();

            if (Ar.Ver >= EUnrealEngineObjectUE3Version.FIXED_AUTO_SHADER_VERSIONING)
            {
                new FSHAHash(Ar);
            }

            InstructionCount = Ar.Read<int>();
        }
    }*/

    public struct FShaderCache
    {
        public EShaderPlatform Platform; // Has Enum conflicts
        public Dictionary<FName, int>? ShaderTypeMap;
        public Dictionary<FName, FTypeSpecificCompressedShaderCode>? CompressedShaderCode;
        public int NumShaders;
        public FShaderCacheShader[] Shaders;

        public FShaderCache(FArchive Ar)
        {
            if (Ar.Ver >= EUnrealEngineObjectUE3Version.GLOBAL_SHADER_FILE)
            {
                Platform = Ar.Read<EShaderPlatform>();

                if (Ar.Ver < EUnrealEngineObjectUE3Version.FIXED_AUTO_SHADER_VERSIONING)
                {
                    ShaderTypeMap = Ar.ReadMap(Ar.ReadFName, Ar.Read<int>);
                }
            }

            if (Ar.Ver >= EUnrealEngineObjectUE3Version.SHADER_COMPRESSION)
            {
                CompressedShaderCode = Ar.ReadMap(Ar.ReadFName, () => new FTypeSpecificCompressedShaderCode(Ar));
            }

            NumShaders = Ar.Read<int>();
            Shaders = new FShaderCacheShader[NumShaders];

            for (int i = 0; i < NumShaders; i++)
            {
                var shader = new FShaderCacheShader
                {
                    ShaderType = Ar.ReadFName(),
                    ShaderId = Ar.Read<FGuid>()
                };

                if (Ar.Ver >= EUnrealEngineObjectUE3Version.FIXED_AUTO_SHADER_VERSIONING)
                {
                    shader.SavedHash = new FSHAHash(Ar);
                }

                var SkipOffset = Ar.Read<int>();

                Shaders[i] = shader;

                // serialization history and FShader is here but skip

                Ar.Seek(SkipOffset, SeekOrigin.Begin);
            }
        }

        public void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            if (ShaderTypeMap?.Count > 0)
            {
                writer.WritePropertyName(nameof(ShaderTypeMap));
                serializer.Serialize(writer, ShaderTypeMap);
            }

            if (CompressedShaderCode?.Count > 0)
            {
                writer.WritePropertyName(nameof(CompressedShaderCode));
                serializer.Serialize(writer, CompressedShaderCode);
            }

            writer.WritePropertyName(nameof(Shaders));
            writer.WriteStartArray();

            foreach (var shader in Shaders)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(FShaderCacheShader.ShaderType));
                serializer.Serialize(writer, shader.ShaderType);

                writer.WritePropertyName(nameof(FShaderCacheShader.ShaderId));
                serializer.Serialize(writer, shader.ShaderId);

                if (shader.SavedHash is { } savedHash)
                {
                    writer.WritePropertyName(nameof(FShaderCacheShader.SavedHash));
                    serializer.Serialize(writer, savedHash);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }

    public struct FShaderCacheShader
    {
        public FName ShaderType;
        public FGuid ShaderId;
        public FSHAHash? SavedHash;
    }

    public class FShaderCacheShaderMap
    {
        public FStaticParameterSet StaticParameterSet;
        public int ShaderMapVersion;
        public int ShaderMapLicenseeVersion;
    }

    public class UShaderCache : Assets.Exports.UObject
    {
        public EShaderPlatform Platform;
        public int ShaderCachePriority;

        public FShaderCache ShaderCache;

        public Dictionary<FName, int>? ShaderTypeMap;
        public Dictionary<FName, int>? VertexFactoryMap;

        public int NumMaterialShaderMaps;
        public FShaderCacheShaderMap[] ShaderMaps;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            if (Ar.Ver > EUnrealEngineObjectUE3Version.SHADER_CACHE_PRIORITY)
            {
                ShaderCachePriority = Ar.Read<int>();
            }

            if (Ar.Ver < EUnrealEngineObjectUE3Version.GLOBAL_SHADER_FILE)
            {
                Platform = Ar.Read<EShaderPlatform>();

                ShaderTypeMap = Ar.ReadMap(Ar.ReadFName, Ar.Read<int>);
                VertexFactoryMap = Ar.ReadMap(Ar.ReadFName, Ar.Read<int>);
            }

            ShaderCache = new FShaderCache(Ar);

            if (Ar.Ver >= EUnrealEngineObjectUE3Version.GLOBAL_SHADER_FILE && Ar.Ver < EUnrealEngineObjectUE3Version.FIXED_AUTO_SHADER_VERSIONING)
            {
                VertexFactoryMap = Ar.ReadMap(Ar.ReadFName, Ar.Read<int>);
            }

            NumMaterialShaderMaps = Ar.Read<int>();
            ShaderMaps = new FShaderCacheShaderMap[NumMaterialShaderMaps];

            for (int i = 0; i < NumMaterialShaderMaps; i++)
            {
                var shaderMap = new FShaderCacheShaderMap
                {
                    StaticParameterSet = new FStaticParameterSet(Ar)
                };

                if (Ar.Ver >= EUnrealEngineObjectUE3Version.UNIFORMEXPRESSION_TEXTUREINDEX)
                {
                    shaderMap.ShaderMapVersion = Ar.Read<int>();
                    shaderMap.ShaderMapLicenseeVersion = Ar.Read<int>();
                }

                var SkipOffset = Ar.Read<int>();
                Ar.Position = SkipOffset;

                ShaderMaps[i] = shaderMap;
            }
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName(nameof(Platform));
            serializer.Serialize(writer, Platform);

            writer.WritePropertyName(nameof(ShaderCache));
            ShaderCache.WriteJson(writer, serializer);

            if (ShaderTypeMap?.Count > 0)
            {
                writer.WritePropertyName(nameof(ShaderTypeMap));
                serializer.Serialize(writer, ShaderTypeMap);
            }

            if (VertexFactoryMap?.Count > 0)
            {
                writer.WritePropertyName(nameof(VertexFactoryMap));
                serializer.Serialize(writer, VertexFactoryMap);
            }

            writer.WritePropertyName(nameof(ShaderMaps));
            serializer.Serialize(writer, ShaderMaps);
        }
    }
}
