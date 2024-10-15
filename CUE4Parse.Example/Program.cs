using System;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace CUE4Parse.Example
{
    public static class Program
    {
        private const string _gameDirectory = "D:\\Games\\Fortnite\\FortniteGame\\Content\\Paks"; // Change game directory path to the one you have.
        private const string _aesKey = "0x6B80868E9345C839D8B10CE00179763E15E5FDA976E499D6CFBEDB41AC0FAD36";

        private const string _mapping = "./mappings.usmap";
        private const string _objectPath = "FortniteGame/Plugins/GameFeatures/BRCosmetics/Content/Athena/Items/Cosmetics/Characters/CID_A_112_Athena_Commando_M_Ruckus.uasset";
        private const string _objectName = "FortCosmeticCharacterPartVariant_0";

        // Rick has 2 exports as of today
        //      - CID_A_112_Athena_Commando_M_Ruckus
        //      - FortCosmeticCharacterPartVariant_0
        //
        // this example will show you how to get them all or just one of them

        public static void Main()
        {
            Log.Logger = new LoggerConfiguration().WriteTo.Console(theme: AnsiConsoleTheme.Literate).CreateLogger();

            var provider = new ApkFileProvider(@"C:\Users\valen\Downloads\ZqOY4K41h0N_Qb6WjEe23TlGExojpQ.apk", true, new VersionContainer(EGame.GAME_UE5_5));
            // var provider = new DefaultFileProvider(_gameDirectory, SearchOption.TopDirectoryOnly, true, new VersionContainer(EGame.GAME_UE5_5));
            // provider.MappingsContainer = new FileUsmapTypeMappingsProvider(_mapping);

            provider.Initialize(); // will scan local files and read them to know what it has to deal with (PAK/UTOC/UCAS/UASSET/UMAP)
            provider.SubmitKey(new FGuid(), new FAesKey(_aesKey)); // decrypt basic info (1 guid - 1 key)

            provider.LoadLocalization(ELanguage.English); // explicit enough

            // these 2 lines will load all exports the asset has and transform them in a single Json string
            var allExports = provider.LoadAllObjects(_objectPath);
            var fullJson = JsonConvert.SerializeObject(allExports, Formatting.Indented);

            // each exports have a name, these 2 lines will load only one export the asset has
            // you must use "LoadObject" and provide the full path followed by a dot followed by the export name
            var variantExport = provider.LoadObject(_objectPath + "." + _objectName);
            var variantJson = JsonConvert.SerializeObject(variantExport, Formatting.Indented);

            Console.WriteLine(variantJson); // Outputs the variantJson.
        }
    }
}
