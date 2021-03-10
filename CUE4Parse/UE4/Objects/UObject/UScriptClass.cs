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
            "AkMediaAssetData" => new UAkMediaAssetData(),
            "BlueprintGeneratedClass" => new UBlueprintGeneratedClass(),
            "CurveTable" => new UCurveTable(),
            "DataTable" => new UDataTable(),
            "Material" => new UMaterial(),
            "MaterialInstanceConstant" => new UMaterialInstanceConstant(),
            "Skeleton" => new USkeleton(),
            "SoundWave" => new USoundWave(),
            "StringTable" => new UStringTable(),
            "Texture2D" => new UTexture2D(),
            "UserDefinedStruct" => new UUserDefinedStruct(),
            "VirtualTexture2D" => new UTexture2D(),
            _ => new Assets.Exports.UObject()
        };
    }
}