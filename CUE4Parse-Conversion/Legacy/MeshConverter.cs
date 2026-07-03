using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Options;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;
using SixLabors.ImageSharp;
using SkiaSharp;
using Serilog;

namespace CUE4Parse_Conversion.Meshes;

[Obsolete("This class is deprecated. Please use the respective DTO constructors directly and handle exceptions as needed.")]
public static class MeshConverter
{
    [Obsolete("This method is deprecated. Please use SkeletonDto constructor directly and handle exceptions as needed.")]
    public static bool TryConvert(this USkeleton originalSkeleton, [MaybeNullWhen(false)] out MeshBoneDto[] bones, out FBox box)
    {
        try
        {
            var converted = new SkeletonDto(originalSkeleton);
            bones = converted.Bones;
            box = converted.Bounds;
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to convert skeleton");
            bones = null;
            box = new FBox();
        }
        return bones != null;
    }

    [Obsolete("This method is deprecated. Please use StaticMeshDto constructor directly and handle exceptions as needed.")]
    public static bool TryConvert(this USplineMeshComponent spline, [MaybeNullWhen(false)] out StaticMeshDto convertedMesh, EMeshQuality quality = EMeshQuality.All)
    {
        try
        {
            convertedMesh = new StaticMeshDto(spline, quality);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to convert spline mesh component");
            convertedMesh = null;
        }
        return convertedMesh != null;
    }

    [Obsolete("This method is deprecated. Please use StaticMeshDto constructor directly and handle exceptions as needed.")]
    public static bool TryConvert(this UStaticMesh originalMesh, [MaybeNullWhen(false)] out StaticMeshDto convertedMesh, EMeshQuality quality = EMeshQuality.All, ENaniteMeshFormat naniteFormat = ENaniteMeshFormat.NoNanite, USplineMeshComponent? spline = null)
    {
        try
        {
            convertedMesh = new StaticMeshDto(originalMesh, quality, naniteFormat, spline);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to convert static mesh");
            convertedMesh = null;
        }
        return convertedMesh != null;
    }

    [Obsolete("This method is deprecated. Please use SkeletalMeshDto constructor directly and handle exceptions as needed.")]
    public static bool TryConvert(this USkeletalMesh originalMesh, [MaybeNullWhen(false)] out SkeletalMeshDto convertedMesh, EMeshQuality quality = EMeshQuality.All)
    {
        try
        {
            convertedMesh = new SkeletalMeshDto(originalMesh, quality);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to convert skeletal mesh");
            convertedMesh = null;
        }
        return convertedMesh != null;
    }

    [Obsolete("This method is deprecated. Please use LandscapeMeshDto constructor directly and handle exceptions as needed.")]
    public static bool TryConvert(this ALandscapeProxy landscape, ULandscapeComponent[]? landscapeComponents, ELandscapeFlags flags, [MaybeNullWhen(false)] out LandscapeMeshDto convertedMesh, out Dictionary<string,Image> heightMaps, out Dictionary<string, SKBitmap> weightMaps)
    {
        heightMaps = [];
        weightMaps = [];

        try
        {
            convertedMesh = new LandscapeMeshDto(landscape, flags, landscapeComponents);

            if (convertedMesh.HeightmapTexture is { } heightmap)
            {
                heightMaps.Add("Heightmap", heightmap);
            }

            if (convertedMesh.BitmapTextures is { } weightmaps)
            {
                foreach (var kv in weightmaps)
                {
                    weightMaps.Add(kv.Key, kv.Value);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to convert landscape mesh");
            convertedMesh = null;
        }
        return convertedMesh != null;
    }
}
