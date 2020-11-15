using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO
{
    public class ObjectIndexHashEntry
    {
        public ObjectIndexHashEntry? Next;
        public string Name = string.Empty;
        public FPackageObjectIndex ObjectIndex;
    }
    
    public class IoGlobalData
    {

        public readonly FNameEntrySerialized[] GlobalNameMap;
        public readonly ObjectIndexHashEntry[] ObjectHashStore;
        public readonly ObjectIndexHashEntry[] ObjectHashHeads;

        public IoGlobalData(IoStoreReader globalReader)
        {
            var nameHashesChunk = globalReader.ChunkIndex(new FIoChunkId(0, 0, EIoChunkType.LoaderGlobalNameHashes));
            var nameCount = (int) (globalReader.TocResource.ChunkOffsetLengths[nameHashesChunk].Length / sizeof(ulong) - 1);
            
            var nameAr = new FByteArchive("LoaderGlobalNames", globalReader.Read(new FIoChunkId(0, 0, EIoChunkType.LoaderGlobalNames)));
            GlobalNameMap = FNameEntrySerialized.LoadNameBatch(nameAr, nameCount);
            
            var metaAr = new FByteArchive("LoaderInitialLoadMeta", globalReader.Read(new FIoChunkId(0, 0, EIoChunkType.LoaderInitialLoadMeta)));

            var numObjects = metaAr.Read<int>();
            var scriptObjects = metaAr.ReadArray<FScriptObjectEntry>(numObjects);
            
            ObjectHashStore = new ObjectIndexHashEntry[numObjects];
            ObjectHashHeads = new ObjectIndexHashEntry[4096];
            for (int i = 0; i < ObjectHashHeads.Length; i++) ObjectHashHeads[i] = new ObjectIndexHashEntry();

            for (int i = 0; i < numObjects; i++)
            {
                ref var e = ref scriptObjects[i];
                
                var scriptName = GlobalNameMap[(int) e.ObjectName.NameIndex];

                var entry = new ObjectIndexHashEntry
                {
                    Name = scriptName.Name, 
                    ObjectIndex = e.GlobalIndex
                };
                ObjectHashStore[i] = entry;

                var hash = ObjectIndexToHash(e.GlobalIndex);
                entry.Next = ObjectHashHeads[hash];
                ObjectHashHeads[hash] = entry;
            }
        }

        public string FindScriptEntryName(FPackageObjectIndex objectIndex)
        {
            var hash = ObjectIndexToHash(objectIndex);
            
            for (var entry = ObjectHashHeads[hash]; entry != null; entry = entry.Next)
            {
                if (entry.ObjectIndex.Value == objectIndex.Value)
                {
                    return entry.Name;
                }
            }
            return "None";
        }

        private const int OBJECT_HASH_BITS = 12;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ObjectIndexToHash(FPackageObjectIndex objectIndex) =>
            (int) ((uint) objectIndex.Value >> (32 - OBJECT_HASH_BITS));
    }
}