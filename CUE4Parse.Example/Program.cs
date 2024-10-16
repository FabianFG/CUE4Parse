using System;
using System.IO;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace CUE4Parse.Example
{
    public static class Program
    {
        private const string GameDirectory = @"D:\Games\Fortnite\FortniteGame\Content\Paks"; // Change game directory path to the one you have.
        private const string AesKey = "0xF271F4B1EA375C42D3676058BAE8FBA295CB61F773070A706A48EAD7C6F98CDB";

        private const string MappingsFile = @"D:\Leaking Tools\FModel\Output\.data\++Fortnite+Release-31.40-CL-36874825-Windows_oo.usmap";
        private const string ObjectPath = "FortniteGame/Content/Athena/Items/Cosmetics/Characters/CID_A_112_Athena_Commando_M_Ruckus";
        private const string ObjectName = "FortCosmeticCharacterPartVariant_0";

        // Rick has 2 exports as of today
        //      - CID_A_112_Athena_Commando_M_Ruckus
        //      - FortCosmeticCharacterPartVariant_0
        //
        // this example will show you how to get them all or just one of them

        public static void Main()
        {
            Log.Logger = new LoggerConfiguration().WriteTo.Console(theme: AnsiConsoleTheme.Literate).CreateLogger();

            var provider = new DefaultFileProvider(GameDirectory, SearchOption.TopDirectoryOnly, true, new VersionContainer(EGame.GAME_UE5_5));
            provider.MappingsContainer = new FileUsmapTypeMappingsProvider(MappingsFile);

            provider.Initialize(); // will scan local files and read them to know what it has to deal with (PAK/UTOC/UCAS/UASSET/UMAP)
            provider.SubmitKey(new FGuid(), new FAesKey(AesKey)); // decrypt basic info (1 guid - 1 key)

            provider.LoadLocalization(); // explicit enough

            // these 2 lines will load all exports the asset has and transform them in a single Json string
            var allExports = provider.LoadAllObjects(ObjectPath);
            var fullJson = JsonConvert.SerializeObject(allExports, Formatting.Indented);

            // each export has a name, these 2 lines will load only one export the asset has
            // you must use "LoadObject" and provide the full path followed by a dot followed by the export name
            var variantExport = provider.LoadObject(ObjectPath + "." + ObjectName);
            var variantJson = JsonConvert.SerializeObject(variantExport, Formatting.Indented);

            Console.WriteLine(variantJson); // Outputs the variantJson.
        }
    }
}
