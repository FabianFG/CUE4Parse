using System;
using System.IO;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using CUE4Parse.MappingsProvider;
using Newtonsoft.Json;

namespace CUE4Parse.Example
{
    public static class Program
    {
        private const string _gameDirectory = "F:\\FortniteGame\\Content\\Paks"; // Change game directory path to the one you have.
        private const string _aesKey = "0xDD1E8B25464C492F0CCECB6740CC0B5C70DF3660D2FBDBD9A23C994256872EB9";
        
        private const string _mapping = "./mappings.usmap";
        private const string _objectPath = "FortniteGame/Content/Athena/Items/Cosmetics/Characters/CID_A_112_Athena_Commando_M_Ruckus";
        private const string _objectName = "FortCosmeticCharacterPartVariant_0";

        // Rick has 2 exports as of today
        //      - CID_A_112_Athena_Commando_M_Ruckus
        //      - FortCosmeticCharacterPartVariant_0
        //
        // this example will show you how to get them all or just one of them
        
        public static void Main(string[] args)
        {
            var provider = new DefaultFileProvider(_gameDirectory, SearchOption.TopDirectoryOnly, true, new VersionContainer(EGame.GAME_UE5_1));
            provider.MappingsContainer = new FileUsmapTypeMappingsProvider(_mapping);

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
