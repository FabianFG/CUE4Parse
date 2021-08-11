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
    }
}