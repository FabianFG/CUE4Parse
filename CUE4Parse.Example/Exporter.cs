using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Sounds;
using CUE4Parse_Conversion.Textures;
using CUE4Parse_Conversion.UEFormat.Enums;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Exports.Wwise;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace CUE4Parse.Example;

[Flags]
public enum ExportType
{
    None       = 0,
    Texture    = 1 << 0,
    Sound      = 1 << 1,
    Mesh       = 1 << 2,
    Animation  = 1 << 3,
}

public static class Exporter
{
    private const string _archiveDirectory = "D:\\Games\\Fortnite\\FortniteGame\\Content\\Paks";
    private const string _aesKey = "0x61D4FD0F3AC7768A08E82A99D275A13762A299FCC28CCF53C46BB221BB90D2B8";
    private const string _mapping = "./++Fortnite+Release-33.20-CL-39082670-Windows_oo.usmap";

    private const string _exportDirectory = "./exports";

    public static void ExportAll() => Export(ExportType.Texture | ExportType.Sound | ExportType.Mesh | ExportType.Animation);

    private static void Export(ExportType type)
    {
        Log.Logger = new LoggerConfiguration().WriteTo.Console(theme: AnsiConsoleTheme.Literate).CreateLogger();

        // same with ZlibHelper
        OodleHelper.DownloadOodleDll();
        OodleHelper.Initialize(OodleHelper.OODLE_DLL_NAME);

        var version = new VersionContainer(EGame.GAME_UE5_6, ETexturePlatform.DesktopMobile);
        var provider = new DefaultFileProvider(_archiveDirectory, SearchOption.TopDirectoryOnly, version)
        {
            MappingsContainer = new FileUsmapTypeMappingsProvider(_mapping)
        };
        provider.Initialize();
        provider.SubmitKey(new FGuid(), new FAesKey(_aesKey));
        provider.PostMount();

        var files = provider.Files.Values
            .GroupBy(it => it.Path.SubstringBeforeLast('/'))
            .ToDictionary(it => it.Key, it => it.ToArray());

        var options = new ExporterOptions
        {
            LodFormat = ELodFormat.FirstLod,
            MeshFormat = EMeshFormat.UEFormat,
            AnimFormat = EAnimFormat.UEFormat,
            MaterialFormat = EMaterialFormat.AllLayersNoRef,
            TextureFormat = ETextureFormat.Png,
            CompressionFormat = EFileCompressionFormat.None,
            Platform = version.Platform,
            SocketFormat = ESocketFormat.Bone,
            ExportMorphTargets = true,
            ExportMaterials = false
        };

        var exportCount = 0;
        var watch = new Stopwatch();
        watch.Start();
        foreach (var (folder, packages) in files)
        {
            Log.Information("scanning {Folder} ({Count} packages)", folder, packages.Length);

            Parallel.ForEach(packages, package =>
            {
                if (!provider.TryLoadPackage(package, out var pkg)) return;

                // optimized way of checking for exports type without loading most of them
                for (var i = 0; i < pkg.ExportMapLength; i++)
                {
                    var pointer = new FPackageIndex(pkg, i + 1).ResolvedObject;
                    if (pointer?.Object is null) continue;

                    var dummy = ((AbstractUePackage) pkg).ConstructObject(pointer.Class?.Object?.Value as UStruct, pkg);
                    switch (dummy)
                    {
                        case UTexture when type.HasFlag(ExportType.Texture) && pointer.Object.Value is UTexture texture:
                        {
                            try
                            {
                                Log.Information("{ExportType} found in {PackageName}", dummy.ExportType, package.Name);
                                SaveTexture(folder, texture, version.Platform, options, ref exportCount);
                            }
                            catch (Exception e)
                            {
                                Log.Warning(e, "failed to decode {TextureName}", texture.Name);
                                return;
                            }
                            break;
                        }
                        case USoundWave when type.HasFlag(ExportType.Sound):
                        case UAkMediaAssetData when type.HasFlag(ExportType.Sound):
                        {
                            Log.Information("{ExportType} found in {PackageName}", dummy.ExportType, package.Name);

                            pointer.Object.Value.Decode(true, out var format, out var bytes);
                            if (bytes is not null)
                            {
                                var fileName = $"{pointer.Object.Value.Name}.{format.ToLower()}";
                                WriteToFile(folder, fileName, bytes, fileName, ref exportCount);
                            }

                            break;
                        }
                        case UAnimSequenceBase when type.HasFlag(ExportType.Animation):
                        case USkeletalMesh when type.HasFlag(ExportType.Mesh):
                        case UStaticMesh when type.HasFlag(ExportType.Mesh):
                        case USkeleton when type.HasFlag(ExportType.Mesh):
                        {
                            Log.Information("{ExportType} found in {PackageName}", dummy.ExportType, package.Name);

                            var exporter = new CUE4Parse_Conversion.Exporter(pointer.Object.Value, options);
                            if (exporter.TryWriteToDir(new DirectoryInfo(_exportDirectory), out _, out var filePath))
                            {
                                WriteToLog(folder, Path.GetFileName(filePath), ref exportCount);
                            }
                            break;
                        }
                    }
                }
            });
        }
        watch.Stop();

        Log.Information("exported {ExportCount} files ({Types}) in {Time}",
            exportCount,
            type.ToStringBitfield(),
            watch.Elapsed);
    }

    private static void SaveTexture(string folder, UTexture texture, ETexturePlatform platform, ExporterOptions options, ref int exportCount)
    {
        var bitmaps = new[] { texture.Decode(platform) };
        switch (texture)
        {
            case UTexture2DArray textureArray:
                bitmaps = textureArray.DecodeTextureArray(platform);
                break;
            case UTextureCube:
                bitmaps[0] = bitmaps[0]?.ToPanorama();
                break;
        }

        var extension = options.TextureFormat.ToString().ToLower();
        foreach (var bitmap in bitmaps)
        {
            if (bitmap is null) continue;
            var bytes = bitmap.Encode(options.TextureFormat, 100).ToArray();
            var fileName = $"{texture.Name}.{extension}";

            WriteToFile(folder, fileName, bytes, $"{fileName} ({bitmap.Width}x{bitmap.Height})", ref exportCount);
        }
    }

    private static void WriteToFile(string folder, string fileName, byte[] bytes, string logMessage, ref int exportCount)
    {
        Directory.CreateDirectory(Path.Combine(_exportDirectory, folder));
        File.WriteAllBytesAsync(Path.Combine(_exportDirectory, folder, fileName), bytes);
        WriteToLog(folder, logMessage, ref exportCount);
    }

    private static void WriteToLog(string folder, string logMessage, ref int exportCount)
    {
        Log.Information("exported {LogMessage} out of {Folder}", logMessage, folder);
        exportCount++;
    }
}
