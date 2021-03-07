using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Exports.Internationalization;
using CUE4Parse.UE4.Assets.Exports.Materials;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Textures;
using CUE4Parse.UE4.Assets.Exports.Wwise;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.UE4.Objects.UObject
{
    // Not an engine class, this inherits the UClass engine class to keep things simple 
    public class UScriptClass : UClass
    {
        public UScriptClass(string className)
        {
            Name = className;
        }

        public Assets.Exports.UObject ConstructObject() => Name switch
        {
            "Texture2D" => new UTexture2D(),
            "VirtualTexture2D" => new UTexture2D(),
            "CurveTable" => new UCurveTable(),
            "DataTable" => new UDataTable(),
            "SoundWave" => new USoundWave(),
            "StringTable" => new UStringTable(),
            "Skeleton" => new USkeleton(),
            "AkMediaAssetData" => new UAkMediaAssetData(),
            "Material" => new UMaterial(),
            "MaterialInstanceConstant" => new UMaterialInstanceConstant(),
            "BlueprintGeneratedClass" => new UBlueprintGeneratedClass(),
            _ => new Assets.Exports.UObject()
        };
    }
}