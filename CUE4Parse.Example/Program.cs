using System.IO;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.Example
{
    public static class Program
    {
        private const string _gameDirectory = "E:\\Fortnite\\Fortnite\\FortniteGame\\Content\\Paks";
        private const string _aesKey = "0x3FE5C589D219E71EE15FEB8FA9BC6B6224BB58AA826A0FA1D997D92E0D8DB23A";
        
        private const string _objectPath = "FortniteGame/Content/Athena/Items/Cosmetics/Characters/CID_A_112_Athena_Commando_M_Ruckus";
        private const string _objectName = "FortCosmeticCharacterPartVariant_0";

        // Rick has 2 exports as of today
        //      - CID_A_112_Athena_Commando_M_Ruckus
        //      - FortCosmeticCharacterPartVariant_0
        //
        // this example will show you how to get them all or just one of them
        
        public static void Main(string[] args)
        {
            var provider = new DefaultFileProvider(_gameDirectory, SearchOption.TopDirectoryOnly, true);
            provider.Initialize(); // will scan local files and read them to know what it has to deal with (PAK/UTOC/UCAS/UASSET/UMAP)
            provider.SubmitKey(new FGuid(), new FAesKey(_aesKey)); // decrypt basic info (1 guid - 1 key)
            
            provider.LoadMappings(); // needed to read Fortnite assets
            provider.LoadLocalization(ELanguage.French); // explicit enough
            
            // these 2 lines will load all exports the asset has and transform them in a single Json string
            var allExports = provider.LoadObjectExports(_objectPath);
            var fullJson = JsonConvert.SerializeObject(allExports, Formatting.Indented);

            // each exports have a name, these 2 lines will load only one export the asset has
            // you must use "LoadObject" and provide the full path followed by a dot followed by the export name
            var variantExport = provider.LoadObject(_objectPath + "." + _objectName);
            var variantJson = JsonConvert.SerializeObject(variantExport, Formatting.Indented);
        }
    }
}