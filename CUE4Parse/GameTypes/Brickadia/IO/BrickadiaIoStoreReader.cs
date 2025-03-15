using System;
using System.Collections.Generic;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.IO;

public partial class IoStoreReader
{
    private void GenerateBrickadiaIndex(StringComparer pathComparer)
    {
        this.bDecrypted = true;
        MountPoint = "";
        ContainerHeader = ReadContainerHeader();
        var files = new Dictionary<string, GameFile>(ContainerHeader.PackageIds.Length, pathComparer);
        foreach (var package in ContainerHeader.PackageIds)
        {
            var packageChunkId = new FIoChunkId(package.id, 0, (byte) EIoChunkType5.ExportBundleData);
            if (TryResolve(packageChunkId, out var offsetLength))
            {
                var packageAr = new FByteArchive("Package", Read((long) offsetLength.Offset, (long) offsetLength.Length), Versions);
                var assetName = FixBrickadiaPackagePath(IoPackage.GetIoPackageName(packageAr), pathComparer);

                if (GetPerfectHashSeedsIndex(packageChunkId, out var index))
                {
                    var name = assetName + ".uasset";
                    files[name] = new FIoStoreEntry(this, name, index);

                    var bulkChunkId = new FIoChunkId(package.id, 0, (byte) EIoChunkType5.BulkData);
                    if (GetPerfectHashSeedsIndex(bulkChunkId, out index))
                    {
                        name = assetName + ".ubulk";
                        files[name] = new FIoStoreEntry(this, name, index);
                    }

                    bulkChunkId = new FIoChunkId(package.id, 0, (byte) EIoChunkType5.OptionalBulkData);
                    if (GetPerfectHashSeedsIndex(bulkChunkId, out index))
                    {
                        name = assetName + ".uptnl";
                        files[name] = new FIoStoreEntry(this, name, index);
                    }
                }
            }
            else
            {
                Log.Warning("Failed to resolve Brickadia package {0}", package.id);
            }
        }
        Files = files;
        return;
    }

    public bool GetPerfectHashSeedsIndex(FIoChunkId chunkId, out uint index)
    {
        if (TocResource.ChunkPerfectHashSeeds != null)
        {
            var chunkCount = TocResource.Header.TocEntryCount;
            if (chunkCount == 0)
            {
                index = default;
                return false;
            }
            var seedCount = (uint) TocResource.ChunkPerfectHashSeeds.Length;
            var seedIndex = (uint) (chunkId.HashWithSeed(0) % seedCount);
            var seed = TocResource.ChunkPerfectHashSeeds[seedIndex];
            if (seed == 0)
            {
                index = default;
                return false;
            }
            uint slot;
            if (seed < 0)
            {
                var seedAsIndex = (uint) (-seed - 1);
                if (seedAsIndex < chunkCount)
                {
                    slot = seedAsIndex;
                }
                else
                {
                    index = default;
                    return false;
                    // Entry without perfect hash
                    //return TryResolveImperfect(chunkId, out outOffsetLength);
                }
            }
            else
            {
                slot = (uint) (chunkId.HashWithSeed(seed) % chunkCount);
            }
            if (TocResource.ChunkIds[slot].GetHashCode() == chunkId.GetHashCode())
            {
                index = slot;
                return true;
            }
        }
        index = default;
        return false;
    }

    private string FixBrickadiaPackagePath(string path, StringComparer PathComparer)
    {
        var root = path.SubstringBefore('/');
        var tree = path.SubstringAfter('/');
        if (PathComparer.Equals(root, "Game"))
        {
            return string.Concat("Brickadia/Content/", tree);
        }
        else if (PathComparer.Equals(root, "Engine"))
        {
            return path;
        }
        else
        {
            return string.Concat("Brickadia/Plugins/", path);
        }
    }
}
