using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Exports.Internationalization;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Exports.Wwise;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.PhysicsEngine;
using CUE4Parse.UE4.Objects.RigVM;
using CUE4Parse.UE4.Objects.UObject;

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
            RegisterClass(typeof(UAnimBlueprintGeneratedClass));
            RegisterClass(typeof(UAnimSequence));
            RegisterClass(typeof(UBlueprintGeneratedClass));
            RegisterClass(typeof(UCurveTable));
            RegisterClass(typeof(UDataTable));
            RegisterClass(typeof(UFunction));
            RegisterClass(typeof(UMaterial));
            RegisterClass(typeof(UMaterialInstanceConstant));
            RegisterClass(typeof(USkeleton));
            RegisterClass(typeof(UStaticMesh));
            RegisterClass(typeof(USkeletalMesh));
            RegisterClass(typeof(USoundWave));
            RegisterClass(typeof(UStringTable));
            RegisterClass(typeof(UTexture2D));
            RegisterClass(typeof(UTextureCube));
            RegisterClass(typeof(ULightMapTexture2D));
            RegisterClass(typeof(UShadowMapTexture2D));
            RegisterClass(typeof(UVolumeTexture));
            RegisterClass(typeof(UUserDefinedEnum));
            RegisterClass(typeof(UUserDefinedStruct));
            RegisterClass(typeof(UWidgetBlueprintGeneratedClass));
            RegisterClass(typeof(UControlRigBlueprintGeneratedClass));
            RegisterClass(typeof(UBodySetup));
            RegisterClass(typeof(UModel));
            RegisterClass(typeof(URigVM));

            // Pre-4.25 property types
            RegisterClass(typeof(UField));
            RegisterClass(typeof(UProperty));
            RegisterClass(typeof(UNumericProperty));
            RegisterClass(typeof(UByteProperty));
            RegisterClass(typeof(UInt8Property));
            RegisterClass(typeof(UInt16Property));
            RegisterClass(typeof(UIntProperty));
            RegisterClass(typeof(UInt64Property));
            RegisterClass(typeof(UUInt16Property));
            RegisterClass(typeof(UUInt32Property));
            RegisterClass(typeof(UUInt64Property));
            RegisterClass(typeof(UFloatProperty));
            RegisterClass(typeof(UDoubleProperty));
            RegisterClass(typeof(UBoolProperty));
            RegisterClass(typeof(UObjectPropertyBase));
            RegisterClass(typeof(UObjectProperty));
            RegisterClass(typeof(UWeakObjectProperty));
            RegisterClass(typeof(ULazyObjectProperty));
            RegisterClass(typeof(USoftObjectProperty));
            RegisterClass(typeof(UClassProperty));
            RegisterClass(typeof(USoftClassProperty));
            RegisterClass(typeof(UInterfaceProperty));
            RegisterClass(typeof(UNameProperty));
            RegisterClass(typeof(UStrProperty));
            RegisterClass(typeof(UArrayProperty));
            RegisterClass(typeof(UMapProperty));
            RegisterClass(typeof(USetProperty));
            RegisterClass(typeof(UStructProperty));
            RegisterClass(typeof(UDelegateProperty));
            RegisterClass(typeof(UMulticastDelegateProperty));
            RegisterClass(typeof(UMulticastInlineDelegateProperty));
            RegisterClass(typeof(UMulticastSparseDelegateProperty));
            RegisterClass(typeof(UEnumProperty));
            RegisterClass(typeof(UTextProperty));
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
                if (!_classes.TryGetValue(serializedName, out var type) && serializedName.EndsWith("_C"))
                {
                    _classes.TryGetValue(serializedName[..^2], out type);
                }
                return type;
            }
        }
        
        public static Type? Get(string serializedName)
        {
            return GetClass(serializedName);
            // TODO add script structs
        }
    }
}