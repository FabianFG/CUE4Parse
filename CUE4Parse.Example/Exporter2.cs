using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.V2;
using CUE4Parse_Conversion.V2.Exporters;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace CUE4Parse.Example;

/// <summary>
/// Example using the new V2 export system with async/parallel support
/// </summary>
public static class Exporter2
{
    private const string _archiveDirectory = "D:\\Games\\Riot Games\\VALORANT\\live\\ShooterGame\\Content\\Paks";
    private const string _aesKey = "0x4BE71AF2459CF83899EC9DC2CB60E22AC4B3047E0211034BBABE9D174C069DD6";
    private const string _mapping = "D:\\FModel\\.data\\VALORANT_12.01_zs.usmap";
    private const string _exportDirectory = "./exports_v2";

    public static void Test()
    {
        ExportAllAsync().GetAwaiter().GetResult();
    }

    public static async Task ExportAllAsync()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(le => le.Properties.ContainsKey("ExporterV2"))
                .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss.fff} {Level:u3} [{ClassName}] {ObjectName}: {Message:lj}{NewLine}{Exception}")
            )
            .WriteTo.Logger(lc => lc
                .Filter.ByExcluding(le => le.Properties.ContainsKey("ExporterV2"))
                .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss.fff} {Level:u3} {Message:lj}{NewLine}{Exception}", theme: AnsiConsoleTheme.Code)
            )
            .CreateLogger();

        await ZlibHelper.InitializeAsync();
        await OodleHelper.InitializeAsync();

        var version = new VersionContainer(EGame.GAME_Valorant);
        var provider = new DefaultFileProvider(_archiveDirectory, SearchOption.TopDirectoryOnly, version)
        {
            MappingsContainer = new FileUsmapTypeMappingsProvider(_mapping)
        };
        provider.Initialize();
        await provider.SubmitKeyAsync(new FGuid(), new FAesKey(_aesKey));
        provider.PostMount();

        var session = new ExportSession(_exportDirectory, new ExporterOptions())
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        var watch = Stopwatch.StartNew();
        // ExportSpecificAssetAsync(provider, session, "ShooterGame/Content/Characters/Clay/S0/3P/Models/TP_Clay_S0_Skelmesh.TP_Clay_S0_Skelmesh");
        ExportFolderAsync(provider, session, "ShooterGame/Content/Characters/Clay");

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            Log.Warning("Cancellation requested...");
        };

        var progress = new Progress<ExportProgress>(p =>
        {
            switch (p.LastResult)
            {
                case { Success: true } result:
                    Log.Information("{Message} [{Progress}]",
                        $"Exported at {result.DiskFilePath}",
                        p.DisplayText);
                    break;
                case { Success: false } failure:
                    Log.Error("Failed to export {ObjectPath}: {Error} [{Progress}]",
                        failure.ObjectPath,
                        failure.Error?.Message ?? "Unknown error",
                        p.DisplayText);
                    break;
            }
        });

        try
        {
            var results = await session.RunAsync(progress, cts.Token);
            watch.Stop();

            var successful = results.Count(r => r.Success);
            var failed = results.Count(r => !r.Success);

            Log.Information("Export completed in {Elapsed}", watch.Elapsed);
            Log.Information("  Successful: {Successful}", successful);
            Log.Information("  Failed: {Failed}", failed);

            // if (failed > 0)
            // {
            //     Log.Warning("Failed exports:");
            //     foreach (var result in results.Where(r => !r.Success))
            //     {
            //         Log.Warning("  - {ObjectName}: {Error}", result.ObjectName, result.Error?.Message);
            //     }
            // }
        }
        catch (OperationCanceledException)
        {
            Log.Warning("Export cancelled by user");
        }
    }

    private static void ExportSpecificAssetAsync(IFileProvider provider, ExportSession session, string assetPath)
    {
        Log.Information("Loading asset: {AssetPath}", assetPath);

        if (!provider.TryLoadPackageObject(assetPath, out var export))
        {
            Log.Warning("Failed to load asset: {AssetPath}", assetPath);
            return;
        }

        AddExporterForAsset(export, session);
    }

    private static void ExportFolderAsync(IFileProvider provider, ExportSession session, string folderPath)
    {
        Log.Information("Scanning folder: {FolderPath}", folderPath);

        var files = provider.Files.Values
            .Where(f => f.Path.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Log.Information("Found {Count} files in {FolderPath}", files.Length, folderPath);

        foreach (var file in files)
        {
            if (!provider.TryLoadPackage(file, out var package))
                continue;

            foreach (var export in package.ExportsLazy)
            {
                AddExporterForAsset(export.Value, session);
            }
        }
    }

    private static void AddExporterForAsset(UObject export, ExportSession session)
    {
        try
        {
            session.Add(export);
        }
        catch
        {
            //
        }
    }
}
