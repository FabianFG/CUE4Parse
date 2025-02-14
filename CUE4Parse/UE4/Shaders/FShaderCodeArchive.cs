using System.Collections.Generic;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Shaders
{
    [JsonConverter(typeof(FShaderCodeArchiveConverter))]
    public class FShaderCodeArchive
    {
        public readonly byte[][] ShaderCode;
        public readonly FRHIShaderLibrary SerializedShaders;
        public readonly Dictionary<FSHAHash, FShaderCodeEntry> PrevCookedShaders;

        public FShaderCodeArchive(FArchive Ar)
        {
            var archiveVersion = Ar.Read<uint>();
            var bIsIoStore = false;

            // version - 1 | Must be I/O Store.
            // version - 2 | Normal pak storage
            if (Ar.Game >= EGame.GAME_UE5_0)
            {
                if (archiveVersion == 1) bIsIoStore = true;
            }

            switch (archiveVersion)
            {
                case 2:
                {
                    var shaders = new FSerializedShaderArchive(Ar);
                    ShaderCode = new byte[shaders.ShaderEntries.Length][];
                    for (var i = 0; i < shaders.ShaderEntries.Length; i++)
                    {
                        ShaderCode[i] = Ar.ReadBytes((int) shaders.ShaderEntries[i].Size);
                    }

                    SerializedShaders = shaders;
                    break;
                }
                case 1 when bIsIoStore: // I/O Store-based ushaderbytecode files start at version 1 now, same as old pak versions.
                {
                    var shaders = new FIoStoreShaderCodeArchive(Ar);
                    // ShaderCode = new byte[shaders.ShaderEntries.Length][];
                    // for (var i = 0; i < shaders.ShaderEntries.Length; i++)
                    // {
                    //     ShaderCode[i] = Ar.ReadBytes((int) shaders.ShaderEntries[i].UncompressedSize);
                    // }

                    SerializedShaders = shaders;

                    break;
                }
                case 1 when !bIsIoStore:
                    // TODO - Need to figure out how this should work
                    // https://github.com/EpicGames/UnrealEngine/blob/4.22/Engine/Source/Runtime/RenderCore/Private/ShaderCodeLibrary.cpp#L910

                    // var mapVarNameNum = Ar.Read<int>();
                    //
                    // PrevCookedShaders = new Dictionary<FSHAHash, FShaderCodeEntry>(mapVarNameNum);
                    // for (var i = 0; i < mapVarNameNum; ++i)
                    // {
                    //     PrevCookedShaders[Ar.Read<FSHAHash>()] = new FShaderCodeEntry(Ar);
                    // }
                    break;
            }
        }
    }
}
