using System;
using System.IO;
using System.Linq;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Images;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Meshes;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace CUE4Parse.Example
{
    public static class Program
    {
        private const string GameDirectory = @"D:\Fortnite\FortniteGame\Content\Paks"; // Change game directory path to the one you have.
        private const string AesKey = "0x6B80868E9345C839D8B10CE00179763E15E5FDA976E499D6CFBEDB41AC0FAD36";

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

            InitOodle();
            
            var provider = new DefaultFileProvider(GameDirectory, SearchOption.TopDirectoryOnly, true, new VersionContainer(EGame.GAME_UE5_5));
            provider.MappingsContainer = new FileUsmapTypeMappingsProvider(MappingsFile);

            provider.Initialize(); // will scan local files and read them to know what it has to deal with (PAK/UTOC/UCAS/UASSET/UMAP)
            provider.SubmitKey(new FGuid(), new FAesKey(AesKey)); // decrypt basic info (1 guid - 1 key)

            provider.LoadLocalization(); // explicit enough

            var customizableObject = provider.LoadObject<UCustomizableObject>("FortniteGame/Plugins/GameFeatures/MeshCosmetics/Content/Jumpsuit/F_MED_Jumpsuit_Scrap/CO/CO_F_MED_Jumpsuit_Scrap.CO_F_MED_Jumpsuit_Scrap");
            var evaluator = new MutableEvaluator(provider, customizableObject);
            
            evaluator.LoadModelStreamable();
            Mesh mesh;
            for (int i = 0; i < customizableObject.Model.Program.Roms.Length; i++)
            {
                var rom = customizableObject.Model.Program.Roms[i];
                if (rom.ResourceType == DataType.DT_IMAGE)
                    continue;
                
                mesh = evaluator.LoadResource((int) rom.ResourceIndex);
                if (mesh.IsBroken == false)
                    return;
            }
            
            evaluator.ReadByteCode();
        }

        public static void InitOodle()
        {
            var oodlePath = Path.Combine(Environment.CurrentDirectory, OodleHelper.OODLE_DLL_NAME);
            if (!File.Exists(oodlePath)) OodleHelper.DownloadOodleDll(oodlePath);
            OodleHelper.Initialize(oodlePath);
        }

        public static void ExportImage(Image image)
        {
            var path = Path.Combine(Environment.CurrentDirectory, "Exports", "export.png");
            var bitmap = image.Decode();
            var encoded = bitmap.Encode(ETextureFormat.Png, 100);
            File.WriteAllBytes(path, encoded.ToArray());
            Console.WriteLine(path);
        }
    }
}
