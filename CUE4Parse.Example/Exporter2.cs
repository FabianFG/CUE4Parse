using System;
using System.IO;
using System.Threading.Tasks;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.V2;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Versions;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace CUE4Parse.Example;

public static class Exporter2
{
    private const string ArchiveDirectory = "D:\\Games\\Riot Games\\VALORANT\\live\\ShooterGame\\Content\\Paks";
    private const string AesKey = "0x4BE71AF2459CF83899EC9DC2CB60E22AC4B3047E0211034BBABE9D174C069DD6";
    private const string Mapping = "D:\\FModel\\.data\\VALORANT_12.01_zs.usmap";
    private const EGame Version = EGame.GAME_Valorant;
    private const string ExportDirectory = "./exports_v2";

    public static void Test()
    {
        PropertyUtil.SearchPropertyInTemplate = true; // search template properties when looking for a prop via GetOrDefault and cie
        DoWork().GetAwaiter().GetResult();
    }

    public static async Task DoWork()
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

        var provider = new DefaultFileProvider(ArchiveDirectory, SearchOption.AllDirectories, new VersionContainer(Version), StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(Mapping))
            provider.MappingsContainer = new FileUsmapTypeMappingsProvider(Mapping);
        provider.Initialize();
        await provider.SubmitKeyAsync(new FGuid(), new FAesKey(AesKey));
        provider.PostMount();
        provider.LoadVirtualPaths();

        var session = new ExportSession(ExportDirectory, new ExporterOptions())
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        const string Map = "Canyon";
        var world = provider.LoadPackageObject<UWorld>($"ShooterGame/Content/Maps/{Map}/{Map}.{Map}");
        session.Add(world);

        var results = await session.RunAsync();
    }
}
