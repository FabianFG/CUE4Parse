using System;
using System.IO;
using System.Linq;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace CUE4Parse.Example;

public static class Program
{
    public static void Main()
    {
        Log.Logger = new LoggerConfiguration().WriteTo.Console(theme: AnsiConsoleTheme.Literate).CreateLogger();

        OodleHelper.DownloadOodleDll();
        OodleHelper.Initialize(OodleHelper.OODLE_DLL_NAME);

        var provider = new DefaultFileProvider("D:\\Fortnite\\FortniteGame\\Content\\Paks", SearchOption.TopDirectoryOnly, new VersionContainer(EGame.GAME_UE5_6));
        provider.MappingsContainer = new FileUsmapTypeMappingsProvider(@"D:\Datamining\FModel\Output\.data\++Fortnite+Release-35.20-CL-42911808-Windows_oo.usmap");
        provider.Initialize();

        provider.SubmitKey(new FGuid("D79AB2C3DF4BEA1654BDFC5904F2B31C"), new FAesKey("0x4FB0E3EB8DDC1F2C3196C8BDBBA696C07F322F8FBF2560702F8B0A691B8C913D"));
        provider.SubmitKey(new FGuid("775056356849367BC0B1B596C264EEC8"), new FAesKey("0x0B0F2A29A8A00D09869C5D0B7CB00E46A886B4E8B2019A11B0028976EF493E91"));
        provider.SubmitKey(new FGuid(), new FAesKey("0x67E992943B63878FEF3C02DE9E0100C127A6C34A569231ED153E03E6CDB0F5A2"));


        foreach (var file in provider.MountedVfs.First(x => x.Name == "pakchunk1006-WindowsClient.utoc").Files.Values)
        {
            if (!file.IsUePackage) continue;

            Log.Information("Loading package: {0}", file.ToString());
            _ = provider.LoadPackage(file).GetExports().ToArray();
        }
    }
}
