using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace CUE4Parse.Example;

public static class Unpacker
{
    private const string _archiveDirectory = "D:\\Games\\Fortnite\\FortniteGame\\Content\\Paks";
    private const string _aesKey = "0x61D4FD0F3AC7768A08E82A99D275A13762A299FCC28CCF53C46BB221BB90D2B8";

    private const string _exportDirectory = "./exports";

    public static void Unpack()
    {
        Log.Logger = new LoggerConfiguration().WriteTo.Console(theme: AnsiConsoleTheme.Literate).CreateLogger();

        // same with ZlibHelper
        OodleHelper.DownloadOodleDll();
        OodleHelper.Initialize(OodleHelper.OODLE_DLL_NAME);

        var version = new VersionContainer(EGame.GAME_UE5_6);
        var provider = new DefaultFileProvider(_archiveDirectory, SearchOption.TopDirectoryOnly, false, version);
        provider.Initialize();
        provider.SubmitKey(new FGuid(), new FAesKey(_aesKey));

        var archive = provider.MountedVfs.First(x => x.Name.Equals("pakchunk0-Windows.pak"));
        var files = archive.Files.Values // provider.Files.Values for all files in all archives
            .GroupBy(it => it.Path.SubstringBeforeLast('/'))
            .ToDictionary(it => it.Key, it => it.ToArray());

        var watch = new Stopwatch();
        watch.Start();
        foreach (var (folder, packages) in files)
        {
            Log.Information("unpacking {Folder} ({Count} packages)", folder, packages.Length);

            Parallel.ForEach(packages, package =>
            {
                var data = provider.SavePackage(package);
                foreach (var (path, bytes) in data)
                {
                    Directory.CreateDirectory(Path.Combine(_exportDirectory, folder));
                    File.WriteAllBytesAsync(Path.Combine(_exportDirectory, path), bytes);
                }
            });
        }
        watch.Stop();

        Log.Information("unpacked {PackageCount} packages in {FolderCount} folders in {Time}",
            files.Values.Sum(it => it.Length),
            files.Count,
            watch.Elapsed);
    }
}
