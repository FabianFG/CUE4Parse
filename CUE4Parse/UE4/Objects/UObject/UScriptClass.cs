using System;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Exports.Internationalization;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Exports.Wwise;
using CUE4Parse.UE4.Objects.Engine;
using Serilog;

namespace CUE4Parse.UE4.Objects.UObject
{
    // Not an engine class, this inherits the UClass engine class to keep things simple 
    public class UScriptClass : UClass
    {
        public UScriptClass(string className)
        {
            Name = className;
        }


        public Assets.Exports.UObject ConstructObject()
        {
            var type = ObjectTypeRegistry.Get(Name);
            if (type != null)
            {
                try
                {
                    var instance = Activator.CreateInstance(type);
                    if (instance is Assets.Exports.UObject obj)
                    {
                        return obj;
                    }
                    else
                    {
                        Log.Warning("Class {Type} did have a valid constructor but does not inherit UObject", type);
                    }
                }
                catch (Exception e)
                {
                    Log.Warning(e, "Class {Type} could not be constructed", type);
                }
            }

            return new Assets.Exports.UObject();
        }
        /*
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
            "WidgetBlueprintGeneratedClass" => new UWidgetBlueprintGeneratedClass(),
            _ => new Assets.Exports.UObject()
        };
        */
    }
}