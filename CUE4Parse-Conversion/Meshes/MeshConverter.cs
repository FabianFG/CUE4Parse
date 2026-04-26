using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CUE4Parse_Conversion.Landscape;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse_Conversion.V2.Dto;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;
using SixLabors.ImageSharp;
using SkiaSharp;
using CUE4Parse.UE4.Assets.Exports.Nanite;
using Serilog;

namespace CUE4Parse_Conversion.Meshes;

/// <summary>
/// TODO: this needs a refactor
/// </summary>
public static class MeshConverter
{
    public static bool TryConvert(this USkeleton originalSkeleton, [MaybeNullWhen(false)] out List<MeshBone> bones, out FBox box)
    {
        try
        {
            var converted = new Skeleton(originalSkeleton);
            bones = converted.RefSkeleton;
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

    public static bool TryConvert(this USplineMeshComponent spline, [MaybeNullWhen(false)] out StaticMesh convertedMesh)
    {
        try
        {
            convertedMesh = new StaticMesh(spline);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to convert spline mesh component");
            convertedMesh = null;
        }
        return convertedMesh != null;
    }

    public static bool TryConvert(this UStaticMesh originalMesh, [MaybeNullWhen(false)] out StaticMesh convertedMesh, ENaniteMeshFormat naniteFormat = ENaniteMeshFormat.OnlyNormalLODs, USplineMeshComponent? spline = null)
    {
        try
        {
            convertedMesh = new StaticMesh(originalMesh, naniteFormat, spline);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to convert static mesh");
            convertedMesh = null;
        }
        return convertedMesh != null;
    }

    public static bool TryConvert(this USkeletalMesh originalMesh, [MaybeNullWhen(false)] out SkeletalMesh convertedMesh)
    {
        try
        {
            convertedMesh = new SkeletalMesh(originalMesh);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to convert skeletal mesh");
            convertedMesh = null;
        }
        return convertedMesh != null;
    }

    public static bool TryConvert(this ALandscapeProxy landscape, ULandscapeComponent[]? landscapeComponents, ELandscapeExportFlags flags, [MaybeNullWhen(false)] out LandscapeMesh convertedMesh, out Dictionary<string,Image> heightMaps, out Dictionary<string, SKBitmap> weightMaps)
    {
        heightMaps = [];
        weightMaps = [];

        try
        {
            convertedMesh = new LandscapeMesh(landscape, flags, landscapeComponents);

            if (convertedMesh.HeightmapTexture is { } heightmap)
            {
                heightMaps.Add("Heightmap", heightmap);
            }

            if (convertedMesh.NormalTexture is { } normal)
            {
                weightMaps.Add("NormalMap_DX", normal);
            }

            if (convertedMesh.WeightmapTextures is { } weightmaps)
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
