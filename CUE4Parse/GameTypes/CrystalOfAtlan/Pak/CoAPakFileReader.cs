using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Pak;

public partial class PakFileReader
{
    private const ulong OFFSET = 0xcbf29ce484222325;
    private const ulong PRIME = 0x00000100000001b3;

    private void CoAReadIndexUpdated(StringComparer pathComparer)
    {
        bDecrypted = true;
        Ar.Position = Info.IndexOffset;
        FArchive primaryIndex = new FByteArchive($"{Name} - Primary Index", ReadAndDecrypt((int) Info.IndexSize));

        EncryptedFileCount = 0;
        var fileCount = primaryIndex.Read<int>();

        string mountPoint;
        try
        {
            mountPoint = primaryIndex.ReadFString();
        }
        catch (Exception e)
        {
            throw new InvalidAesKeyException($"Given aes key '{AesKey?.KeyString}' is not working with '{Name}'", e);
        }

        ValidateMountPoint(ref mountPoint);
        MountPoint = mountPoint;
        var bytes = primaryIndex.ReadBytes(3);
        var PathHashSeed = primaryIndex.Read<ulong>();
        
        var encodedPakEntriesSize = primaryIndex.Read<int>();
        var encodedPakEntries = primaryIndex.ReadBytes(encodedPakEntriesSize);
        var encodedEntries = new FByteArchive("Encoded Entries", encodedPakEntries);

        primaryIndex.Position += 8;
        if (!primaryIndex.ReadBoolean())
            throw new ParserException(primaryIndex, "No directory index");
        var directoryIndexOffset = primaryIndex.Read<long>();
        var directoryIndexSize = primaryIndex.Read<long>();

        Ar.Position = directoryIndexOffset;
        var directoryIndex = new FByteArchive($"{Name} - Directory Index", ReadAndDecrypt((int) directoryIndexSize));
        var directoryIndexLength = directoryIndex.Read<int>();

        string FixCoAPackagePath(string path, StringComparer PathComparer)
        {
            var root = path.SubstringBefore('/');
            var tree = path.SubstringAfter('/');
            if (PathComparer.Equals(root, "Game"))
            {
                return string.Concat("Seria/Content/", tree);
            }
            else if (PathComparer.Equals(root, "Engine"))
            {
                return path;
            }
            else
            {
                return string.Concat("Seria/Plugins/", path);
            }
        };

        var files = new Dictionary<string, GameFile>(fileCount, pathComparer);
        if (directoryIndexLength == 0)
        {
            var pathHashIndex = directoryIndex.ReadMap(directoryIndex.Read<ulong>, directoryIndex.Read<int>);
            var used = new HashSet<ulong>();
            foreach (var key in pathHashIndex)
            {
                var hash = key.Key;
                var offset = key.Value;
                if (used.Contains(hash) || offset == int.MinValue)
                    continue;

                var entry = new FPakEntry(this, hash.ToString(), encodedEntries, offset);

                if (!entry.TryCreateReader(out var reader) || reader.Read<uint>() != FPackageFileSummary.PACKAGE_FILE_TAG) continue;

                reader.Seek(0, SeekOrigin.Begin);
                var package = new Package(reader, null, new Lazy<FArchive?>());

                var exports = package.ExportMap.Where(export => export.IsAsset).ToList();
                var assetname = exports.Count == 1 ? exports[0].ObjectName.Text : exports.FirstOrDefault(exp => (exp.ObjectFlags & 2) == 2)?.ObjectName.Text;
                if (assetname is null)
                {
                    Log.Warning("Can't find package name for {0} pathhash", hash);
                    continue;
                }

                if (assetname.EndsWith("_C")) assetname = assetname[..^2];
                var name = package.NameMap.FirstOrDefault(name => pathComparer.Equals(name.Name.SubstringAfterLast('/'), assetname) && name.Name.StartsWith('/'));
                if (name.Name is null)
                {
                    Log.Warning("Can't find package name for {0} pathhash", hash);
                    continue;
                }

                var packageName = FixCoAPackagePath(name.Name[1..], pathComparer);
                var hashpath = packageName.Replace(MountPoint, "");
                var path = string.Concat(MountPoint, hashpath, ".uasset");
                entry.Path = path;
                if (entry.IsEncrypted)
                    EncryptedFileCount++;
                files[path] = entry;
                used.Add(hash);

                void FindPayload(FArchive Ar, string extension, bool warning = false)
                {
                    var payloadName = string.Concat(hashpath, ".", extension);
                    var hash = Fnv64Path(payloadName, PathHashSeed);
                    if (pathHashIndex.TryGetValue(hash, out offset))
                    {
                        if (offset != int.MinValue)
                        {
                            path = string.Concat(MountPoint, payloadName);
                            entry = new FPakEntry(this, path, Ar, offset);
                            if (entry.IsEncrypted)
                                EncryptedFileCount++;
                            files[path] = entry;
                            used.Add(hash);
                        }
                    }
                    else if (warning)
                    {
                        Log.Warning("Missing {0} file for package {1}", extension, packageName);
                    }
                }

                FindPayload(encodedEntries, "uexp", true);
                FindPayload(encodedEntries, "ubulk");
                FindPayload(encodedEntries, "uptnl");
            }

            foreach (var hash in pathHashIndex.Keys.Except(used))
            {
                var name = hash.ToString();
                string path = string.Concat(mountPoint, name);

                var offset = pathHashIndex[hash];
                if (offset == int.MinValue)
                    continue;

                var entry = new FPakEntry(this, path, encodedEntries, offset);
                if (entry.IsEncrypted)
                    EncryptedFileCount++;
                files[path] = entry;
                Log.Warning("Can't find corresponding name for {0} pathhash", hash);
            }

            Files = files;
            return;
        }

        for (var i = 0; i < directoryIndexLength; i++)
        {
            var dir = directoryIndex.ReadFString();
            var dirDictLength = directoryIndex.Read<int>();

            for (var j = 0; j < dirDictLength; j++)
            {
                var name = directoryIndex.ReadFString();
                string path;
                if (mountPoint.EndsWith('/') && dir.StartsWith('/'))
                    path = dir.Length == 1 ? string.Concat(mountPoint, name) : string.Concat(mountPoint, dir[1..], name);
                else
                    path = string.Concat(mountPoint, dir, name);

                var offset = directoryIndex.Read<int>();
                if (offset == int.MinValue)
                    continue;

                var entry = new FPakEntry(this, path, encodedEntries, offset);
                if (entry.IsEncrypted)
                    EncryptedFileCount++;
                files[path] = entry;
            }
        }

        Files = files;
    }

    public static ulong Fnv64(byte[] data, ulong seed)
    {
        ulong hash = OFFSET + seed;
        foreach (byte b in data)
        {
            hash ^= b;
            hash *= PRIME;
        }
        return hash;
    }

    public static ulong Fnv64Path(string path, ulong seed)
    {
        string lower = path.ToLower();
        byte[] data = Encoding.Unicode.GetBytes(lower);
        return Fnv64(data, seed);
    }
}
