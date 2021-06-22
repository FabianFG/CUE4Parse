using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Exports.Internationalization;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Exports.Wwise;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.PhysicsEngine;

namespace CUE4Parse.UE4.Assets
{
    public static class ObjectTypeRegistry
    {
        private static Dictionary<string, Type> _classes = new();

        static ObjectTypeRegistry()
        {
            RegisterEngine();
        }

        private static void RegisterEngine()
        {
            RegisterClass(typeof(UAkMediaAssetData)); // todo should this be here??? Yes
            RegisterClass(typeof(UBlueprintGeneratedClass));
            RegisterClass(typeof(UCurveTable));
            RegisterClass(typeof(UDataTable));
            RegisterClass(typeof(UMaterial));
            RegisterClass(typeof(UMaterialInstanceConstant));
            RegisterClass(typeof(USkeleton));
            RegisterClass(typeof(UStaticMesh));
            RegisterClass(typeof(USoundWave));
            RegisterClass(typeof(UStringTable));
            RegisterClass(typeof(UTexture2D));
            RegisterClass(typeof(UWidgetBlueprintGeneratedClass));
            RegisterClass(typeof(UBodySetup));
            RegisterClass(typeof(UModel));
        }

        public static void RegisterClass(Type type)
        {
            var name = type.Name;
            if ((name[0] == 'U' || name[0] == 'A') && char.IsUpper(name[1]))
                name = name.Substring(1);
            RegisterClass(name, type);
        }

        public static void RegisterClass(string serializedName, Type type)
        {
            lock (_classes)
            {
                _classes[serializedName] = type;
            }
        }

        public static Type? GetClass(string serializedName)
        {
            lock (_classes)
            {
                return _classes.TryGetValue(serializedName, out var type) ? type : null;
            }
        }
        
        public static Type? Get(string serializedName)
        {
            return GetClass(serializedName);
            // TODO add script structs
        }
    }
}