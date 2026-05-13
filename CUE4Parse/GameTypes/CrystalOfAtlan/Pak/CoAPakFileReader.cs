using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;
using GenericReader;

namespace CUE4Parse.UE4.Pak;

public static class CoAPlugins
{
    public static readonly byte[] returnbytes = [0x72, 0x65, 0x74, 0x75, 0x72, 0x6e];
    public static readonly Dictionary<string, string> CoAVirtualPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        { "UnLua" , "Seria/Plugins/UnLua" },
        { "ABR" , "Engine/Plugins/ABR" },
        { "Simplygon" , "Seria/Plugins/Simplygon" },
        { "SpeedTreeImporter" , "Engine/Plugins/Editor/SpeedTreeImporter" },
        { "PhysicalMaterialUtilities" , "Engine/Plugins/Editor/PhysicalMaterialUtilities" },
        { "ControlRig" , "Engine/Plugins/Experimental/ControlRig" },
        { "EcoSystem" , "Seria/Plugins/EcoSystem" },
        { "TAToolset" , "Seria/Plugins/TAToolset" },
        { "ServerHotReload" , "Seria/Plugins/ServerHotReload" },
        { "FSR2" , "Engine/Plugins/Runtime/Amd/FSR2" },
        { "MontageAutoGenerator" , "Seria/Plugins/SeriaMontageAutoGenerator/MontageAutoGenerator" },
        { "FrameGeneration" , "Engine/Plugins/FrameGeneration" },
        { "SeriaAssetLoad" , "Seria/Plugins/SeriaAssetLoad" },
        { "CustomCUIActionRouter" , "Seria/Plugins/CustomCUIActionRouter" },
        { "SequenceDialogue" , "Seria/Plugins/SequenceDialogue" },
        { "MicroService" , "Seria/Plugins/MicroService" },
        { "UnrealHDiffPatch" , "Seria/Plugins/UnrealHDiffPatch" },
        { "LimitMultiInstance" , "Seria/Plugins/LimitMultiInstance" },
        { "ServerProfiler" , "Seria/Plugins/ServerProfiler" },
        { "CascadeToNiagaraConverter" , "Engine/Plugins/FX/CascadeToNiagaraConverter" },
        { "Wwise" , "Seria/Plugins/Wwise" },
        { "HotPatcher" , "Seria/Plugins/HotPatcher" },
        { "SeriaEditor" , "Seria/Plugins/SeriaEditor" },
        { "OasisUENative" , "Seria/Plugins/OasisUENative" },
        { "SunPosition" , "Engine/Plugins/Runtime/SunPosition" },
        { "DLSS" , "Engine/Plugins/Nvidia/DLSS" },
        { "SGSR" , "Engine/Plugins/Runtime/SGSR" },
        { "CommonUI" , "Engine/Plugins/Experimental/CommonUI" },
        { "EnvSystem" , "Seria/Plugins/EnvSystem" },
        { "ConfigReader" , "Seria/Plugins/ConfigReader" },
        { "SeriaPluginAssets" , "Seria/Plugins/SeriaPluginAssets" },
        { "GameExtension" , "Seria/Plugins/GameExtension" },
        { "TimeOfDay" , "Seria/Plugins/TimeOfDay" },
        { "PythonScriptPlugin" , "Engine/Plugins/Experimental/PythonScriptPlugin" },
        { "Water" , "Seria/Plugins/Water" },
        { "AdvanceVibration" , "Seria/Plugins/AdvanceVibration" },
        { "WindowWebView" , "Seria/Plugins/WindowWebView" },
        { "NiagaraUIRenderer" , "Seria/Plugins/NiagaraUIRenderer" },
        { "Mover" , "Seria/Plugins/Mover" },
        { "SeriaAbilityHelper" , "Seria/Plugins/SeriaAbilityHelper" },
        { "MediaCompositing" , "Engine/Plugins/Media/MediaCompositing" },
        { "LevelStreamAssistant" , "Seria/Plugins/LevelStreamAssistant" },
        { "SeriaMigrate" , "Seria/Plugins/SeriaMigrate" },
        { "SpinePlugin" , "Seria/Plugins/SpinePlugin" },
        { "GeometryMode" , "Engine/Plugins/Editor/GeometryMode" },
        { "StatVisualizationMode" , "Seria/Plugins/StatVisualizationMode" },
        { "GeometryProcessing" , "Engine/Plugins/Experimental/GeometryProcessing" },
        { "Landmass" , "Engine/Plugins/Experimental/Landmass" },
        { "DataValidationExternal" , "Seria/Plugins/DataValidationExternal" },
        { "WeLingPipeSDK" , "Seria/Plugins/WeLingPipeSDK" },
        { "Paper2D" , "Engine/Plugins/2D/Paper2D" },
        { "SeriaSVNUtils" , "Seria/Plugins/SeriaSVNUtils" },
        { "ACLPlugin" , "Seria/Plugins/ACLPlugin" },
        { "VFXAssetChecker" , "Seria/Plugins/SeriaVFXAssetChecker/VFXAssetChecker" },
        { "RemoteSession" , "Engine/Plugins/Experimental/RemoteSession" },
        { "SeriaNiagara" , "Seria/Plugins/SeriaNiagara" },
        { "QRCodeGen" , "Seria/Plugins/QRCodeGen" },
        { "ObjectContainers" , "Seria/Plugins/ObjectContainers" },
        { "SeriaPreLoadScreen" , "Seria/Plugins/SeriaPreLoadScreen" },
        { "AssetHardRefConverter" , "Seria/Plugins/AssetHardRefConverter" },
        { "HmsXegSpatialUpscale" , "Engine/Plugins/HmsXegSpatialUpscale" },
        { "SeriaDialogEditor" , "Seria/Plugins/SeriaDialogEditor" },
        { "Niagara" , "Engine/Plugins/FX/Niagara" },
        { "SeriaAnimation" , "Seria/Plugins/SeriaAnimation" },
        { "MontageTask" , "Seria/Plugins/MontageTask" },
        { "SeriaCustomCrashCollector" , "Engine/Plugins/SeriaCustomCrashCollector" },
        { "HDRIBackdrop" , "Engine/Plugins/Runtime/HDRIBackdrop" },
        { "MeshPainting" , "Engine/Plugins/MeshPainting" },
        { "FileDownloader" , "Seria/Plugins/FileDownloader" },
        { "MontageGenerator" , "Seria/Plugins/SeriaMontageGenerator/MontageGenerator" },
        { "SeriaUMG" , "Seria/Plugins/SeriaUMG" },
        { "ShootsStub" , "Seria/Plugins/ShootsStub" },
        { "ImpostorBaker" , "Seria/Plugins/ImpostorBaker" },
        { "TraceSourceFilters" , "Engine/Plugins/Developer/TraceSourceFiltering" },
        { "SeriaAssetUpdate" , "Seria/Plugins/SeriaAssetUpdate" },
        { "PythonUtils" , "Seria/Plugins/PythonUtils" },
    }; 
}

public partial class PakFileReader
{
    private const ulong OFFSET = 0xcbf29ce484222325;
    private const ulong PRIME = 0x00000100000001b3;

    private void CoAReadIndexUpdated(StringComparer pathComparer)
    {
        bDecrypted = true;
        Ar.Position = Info.IndexOffset;
        using FArchive primaryIndex = new FByteArchive($"{Name} - Primary Index", ReadAndDecrypt((int) Info.IndexSize));

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
        using var encodedEntries = new GenericBufferReader(primaryIndex.ReadBytes(encodedPakEntriesSize));
        primaryIndex.Position += 8;
        if (!primaryIndex.ReadBoolean())
            throw new ParserException(primaryIndex, "No directory index");
        var directoryIndexOffset = primaryIndex.Read<long>();
        var directoryIndexSize = primaryIndex.Read<long>();

        Ar.Position = directoryIndexOffset;
        using var directoryIndex = new FByteArchive($"{Name} - Directory Index", ReadAndDecrypt((int) directoryIndexSize));
        var directoryIndexLength = directoryIndex.Read<int>();

        string FixCoAPackagePath(string path, StringComparer PathComparer)
        {
            var span = path.AsSpan()[1..];
            var index = span.IndexOf('/');
            var root = span[..index];
            var tree = span[(index + 1)..];
            if (root.Equals("Game", StringComparison.OrdinalIgnoreCase))
            {
                return string.Concat("Seria/Content/", tree);
            }
            else if (root.Equals("Engine", StringComparison.OrdinalIgnoreCase))
            {
                return string.Concat("Engine/Content/", tree);
            }
            else
            {
                var rootstring = root.ToString();
                if (CoAPlugins.CoAVirtualPaths.TryGetValue(rootstring, out var use))
                {
                    return string.Concat(use, "/Content/", tree);
                }
                return path;
            }
        }

        var total = 0;
        var files = new Dictionary<string, GameFile>(fileCount, pathComparer);
        var entries = new Dictionary<int, string>();
        if (directoryIndexLength > 0 )
        {
            for (var i = 0; i < directoryIndexLength; i++)
            {
                var dir = directoryIndex.ReadFString();
                var dirDictLength = directoryIndex.Read<int>();
                total += dirDictLength;
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
                    entries[offset] = path;
                    if (entry.IsEncrypted)
                        EncryptedFileCount++;
                    files[path] = entry;
                }
            }
        }

        if (total < fileCount)
        {
            var regex = new Regex(@"^(0|[1-9]\d*)$");
            var pathHashIndex = directoryIndex.ReadMap(directoryIndex.Read<ulong>, directoryIndex.Read<int>);
            var used = new HashSet<ulong>();

            void FindPayload(GenericBufferReader Ar, string path, string extension, bool warning = false)
            {
                var payloadName = System.IO.Path.ChangeExtension(path, extension);
                var hash = Fnv64Path(payloadName, PathHashSeed);
                if (pathHashIndex.TryGetValue(hash, out var offset))
                {
                    if (offset != int.MinValue && !entries.ContainsKey(offset))
                    {
                        path = string.Concat(MountPoint, payloadName);
                        var entry = new FPakEntry(this, path, Ar, offset);
                        if (entry.IsEncrypted)
                            EncryptedFileCount++;
                        files[path] = entry;
                        used.Add(hash);
                    }
                }
                else if (warning)
                {
                    Log.Warning("Missing {0} file for package {1}", extension, path);
                }
            }

            List<string> foundFiles = [];
            var packageNamesFile = System.IO.Path.Combine(System.IO.Path.ChangeExtension(Path, "txt"));
            var parentDir = Directory.GetParent(Path)?.Parent;
            if (parentDir is not null)
            {
                var baseFileName = System.IO.Path.GetFileNameWithoutExtension(Path);
                packageNamesFile = System.IO.Path.Combine(parentDir.FullName, "GeneratedIndex", baseFileName + ".txt");
            }

            if (System.IO.Path.Exists(packageNamesFile))
            {
                var allLines = File.ReadAllLines(packageNamesFile);
                foundFiles.AddRange(allLines);
                foreach (var line in allLines)
                {
                    var newhash = Fnv64Path(line, PathHashSeed);
                    if (pathHashIndex.TryGetValue(newhash, out var offset))
                    {
                        if (entries.ContainsKey(offset))
                        {
                            FindPayload(encodedEntries, line, "uexp", true);
                            FindPayload(encodedEntries, line, "ubulk");
                            FindPayload(encodedEntries, line, "uptnl");
                            continue;
                        }

                        if (offset != int.MinValue && !entries.ContainsKey(offset))
                        {
                            var filepath = string.Concat(MountPoint, line);
                            var entry = new FPakEntry(this, filepath, encodedEntries, offset);
                            if (entry.IsEncrypted)
                                EncryptedFileCount++;
                            files[filepath] = entry;
                            used.Add(newhash);

                            FindPayload(encodedEntries, line, "uexp", true);
                            FindPayload(encodedEntries, line, "ubulk");
                            FindPayload(encodedEntries, line, "uptnl");
                        }
                    }
                }
            }

            foreach (var key in pathHashIndex)
            {
                var hash = key.Key;
                var offset = key.Value;
                if (entries.ContainsKey(offset))
                {
                    used.Add(hash);
                    continue;
                }
                if (used.Contains(hash) || offset == int.MinValue)
                    continue;

                var entry = new FPakEntry(this, hash.ToString(), encodedEntries, offset);

                if (!entry.TryRead(out var data) || entry.UncompressedSize < 4)
                {
                    Log.Warning("Failed to create reader for pathhash {0} with offset {1}", hash, offset);
                    files[hash.ToString()] = entry;
                    continue;
                }

                using var reader = new FByteArchive("", data);
                var magic = reader.Read<uint>();
                if (MountPoint == "")
                    mountPoint = "Seria/Content/Seria/";
                string? filepath = null;
                switch (magic)
                {
                    case 0x61754c1b:
                        reader.Position += 29;
                        filepath = string.Concat(mountPoint, reader.ReadString())[..^1];
                        break;
                    case 0x4f54544f or 0x474e5089 or 2:
                        var extension = magic switch
                        {
                            0x4f54544f => ".otf",
                            0x474e5089 => ".png",
                            2 => ".ushaderbytecode",
                        };
                        filepath = string.Concat(mountPoint, hash, extension);
                        break;
                    case FPackageFileSummary.PACKAGE_FILE_TAG:
                        break;
                    default:
                        if (data.AsSpan().IndexOf(CoAPlugins.returnbytes) >= 0)
                        {
                            filepath = string.Concat(mountPoint, hash, ".lua");
                            break;
                        }
                        continue;
                }
                if (!string.IsNullOrEmpty(filepath))
                {
                    entry.Path = filepath;
                    if (entry.IsEncrypted) EncryptedFileCount++;
                    files[filepath] = entry;
                    used.Add(hash);
                    continue;
                }

                reader.Seek(0, SeekOrigin.Begin);
                var package = new Package(reader, null, _ => null);

                var exports = package.ExportMap.Where(export => export.IsAsset).ToList();
                FObjectExport? mainExport;
                if (exports.Count == 1)
                {
                    mainExport = exports[0];
                }
                else
                {
                    mainExport = exports.FirstOrDefault(exp => (exp.ObjectFlags & 2) == 2);
                    if (mainExport is null)
                    {
                        Log.Warning("Can't find export name for {0} pathhash", hash);
                        continue;
                    }
                }

                (string assetname, int number) = (mainExport.ObjectName.PlainText, mainExport.ObjectName.Number);
                if (assetname.EndsWith("_C") && mainExport.ClassName.EndsWith("BlueprintGeneratedClass", StringComparison.OrdinalIgnoreCase))
                    assetname = assetname[..^2];
                if (assetname.EndsWith("-atlas") && mainExport.ClassName.EndsWith("AtlasAsset", StringComparison.OrdinalIgnoreCase) && exports.Count > 1)
                    assetname = assetname[..^6];

                var numberIndex = assetname.LastIndexOf('_');
                if (number == 0 && numberIndex != -1 && numberIndex > 0)
                {
                    if (numberIndex < assetname.Length - 1)
                        numberIndex++;
                    if (regex.IsMatch(assetname.AsSpan()[numberIndex..]))
                    {
                        if (assetname[numberIndex] != '0')
                        {
                            if (int.TryParse(assetname.AsSpan()[numberIndex..], out var number1) && number1 >= 0)
                            {
                                number = number1 + 1;
                                assetname = assetname[..(numberIndex - 1)];
                            }
                        }
                        else if (int.TryParse(assetname.AsSpan()[numberIndex..], out var number2) && number2 == 0)
                        {
                            number = 1;
                            assetname = assetname[..(numberIndex - 1)];
                        }
                    }
                }

                var found = false;
                var path = string.Empty;
                var hashpath = string.Empty;
                string packageName = "";
                ulong hash1 = 0;
                ulong hash2 = 0;
                foreach (var name in package.NameMap)
                {
                    if (!name.Name.StartsWith('/') || name.Name.Length <= 1 || name.Name.StartsWith("/Script"))
                        continue;
                    packageName = FixCoAPackagePath(number == 0 ? name.Name : $"{name.Name}_{number - 1}", pathComparer);
                    hashpath = MountPoint == ""
                        ? packageName
                        : packageName.StartsWith(MountPoint) ? packageName.Substring(MountPoint.Length) : packageName;
                    hash1 = Fnv64Path(hashpath + ".uasset", PathHashSeed);
                    if (hash1 == hash)
                    {
                        found = true;
                        hashpath += ".uasset";
                        path = string.Concat(MountPoint, hashpath);
                        break;
                    }

                    hash2 = Fnv64Path(hashpath + ".umap", PathHashSeed);
                    if (hash2 == hash)
                    {
                        found = true;
                        hashpath += ".umap";
                        path = string.Concat(MountPoint, hashpath);
                        break;
                    }
                }

                if (!found)
                {
                    Log.Warning("Can't find package name for {0} pathhash in {1}", hash, Name);
                    continue;
                }

                foundFiles.Add(hashpath);
                entry.Path = path;
                if (entry.IsEncrypted)
                    EncryptedFileCount++;
                files[path] = entry;
                used.Add(hash);

                FindPayload(encodedEntries, hashpath, "uexp", true);
                FindPayload(encodedEntries, hashpath, "ubulk");
                FindPayload(encodedEntries, hashpath, "uptnl");
            }

            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(packageNamesFile)!);
            File.WriteAllLinesAsync(packageNamesFile, foundFiles);

            if (MountPoint == "")
                mountPoint = "Seria/Content/";
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
