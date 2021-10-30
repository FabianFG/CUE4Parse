using System.Collections.Generic;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.IO
{
    public class IoGlobalData
    {
        public readonly FNameEntrySerialized[] GlobalNameMap;
        public readonly Dictionary<FPackageObjectIndex, FScriptObjectEntry> ScriptObjectEntriesMap = new();

        public IoGlobalData(IoStoreReader globalReader)
        {
            FByteArchive metaAr;
            if (globalReader.Game >= EGame.GAME_UE5_0)
            {
                metaAr = new FByteArchive("ScriptObjects", globalReader.Read(new FIoChunkId(0, 0, EIoChunkType5.ScriptObjects)));
                GlobalNameMap = FNameEntrySerialized.LoadNameBatch(metaAr);
            }
            else // UE4.26+
            {
                if (!globalReader.TryResolve(new FIoChunkId(0, 0, EIoChunkType.LoaderGlobalNameHashes), out var nameHashesChunk))
                {
                    throw new KeyNotFoundException("Couldn't find LoaderGlobalNameHashes chunk in IoStore " + globalReader.Name);
                }

                var nameCount = (int) (nameHashesChunk.Length / sizeof(ulong) - 1);

                var nameAr = new FByteArchive("LoaderGlobalNames", globalReader.Read(new FIoChunkId(0, 0, EIoChunkType.LoaderGlobalNames)));
                GlobalNameMap = FNameEntrySerialized.LoadNameBatch(nameAr, nameCount);

                metaAr = new FByteArchive("LoaderInitialLoadMeta", globalReader.Read(new FIoChunkId(0, 0, EIoChunkType.LoaderInitialLoadMeta)));
            }

            var numScriptObjects = metaAr.Read<int>();
            var scriptObjectEntries = metaAr.ReadArray<FScriptObjectEntry>(numScriptObjects);
            foreach (var scriptObjectEntry in scriptObjectEntries)
            {
                ScriptObjectEntriesMap[scriptObjectEntry.GlobalIndex] = scriptObjectEntry;
            }
        }
    }
}