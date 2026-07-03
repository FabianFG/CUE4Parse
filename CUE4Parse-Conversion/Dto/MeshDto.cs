using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Writers;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;
using CUE4Parse.UE4.Assets.Exports.Nanite;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;

namespace CUE4Parse_Conversion.Dto;

public abstract class MeshDto<TVertex> : ObjectDto where TVertex : struct, IMeshVertex
{
    public readonly IList<MeshLodDto<TVertex>> LODs = [];
    public readonly MeshMaterialDto[] Materials = [];

    public FPackageIndex[]? Sockets { get; private set; }
    public abstract FBox Bounds { get; protected init; }

    protected MeshDto(UObject owner) : base(owner)
    {

    }

    protected MeshDto(UStaticMesh mesh) : base(mesh)
    {
        Materials = new MeshMaterialDto[mesh.StaticMaterials.Length];
        for (var i = 0; i < Materials.Length; i++)
        {
            Materials[i] = new MeshMaterialDto(mesh.StaticMaterials[i]);
        }
        Sockets = mesh.Sockets;
    }

    protected MeshDto(USkeletalMesh mesh) : base(mesh)
    {
        Materials = new MeshMaterialDto[mesh.SkeletalMaterials.Length];
        for (var i = 0; i < Materials.Length; i++)
        {
            Materials[i] = new MeshMaterialDto(mesh.SkeletalMaterials[i]);
        }

        var sockets = new List<FPackageIndex>(mesh.Sockets);
        if (mesh.Skeleton.TryLoad<USkeleton>(out var skeleton))
        {
            sockets.AddRange(skeleton.Sockets);
        }
        Sockets = sockets.ToArray();
    }

    protected MeshDto(USkeleton skeleton) : base(skeleton)
    {
        Sockets = skeleton.Sockets;
    }

    protected MeshDto(ALandscapeProxy landscape) : base(landscape)
    {
        Materials = [new MeshMaterialDto(null, landscape.LandscapeMaterial)];
    }

    public MeshMaterialDto? GetMaterial(MeshSectionDto section)
    {
        var index = section.MaterialIndex;
        if (index < 0 || index >= Materials.Length)
        {
            return null;
        }

        return Materials[index];
    }

    protected void SetLodSuffixes()
    {
        // suffix is used for writing to disk
        // the file with no suffix is considered the main quality, that's what world exporter references
        // nanite lod always has SourceLodIndex 0, same as real lod 0, so we can't key off SourceLodIndex or they would collide
        // so use position in the list instead

        for (var i = 0; i < LODs.Count; i++)
        {
            var lod = LODs[i];
            lod._suffix = i == 0 ? null : lod.IsNanite ? "_Nanite" : $"_LOD{lod.SourceLodIndex}";
        }
    }

    public override void Dispose()
    {
        LODs.Clear();
        Array.Clear(Materials);
        Sockets = null;
    }
}

public class StaticMeshDto : MeshDto<MeshVertex>
{
    public FPackageIndex? BodySetup { get; private set; }
    public sealed override FBox Bounds { get; protected init; }

    protected StaticMeshDto(UObject owner) : base(owner)
    {

    }

    public StaticMeshDto(UStaticMesh mesh, EMeshQuality quality = EMeshQuality.All, ENaniteMeshFormat naniteFormat = ENaniteMeshFormat.NoNanite, USplineMeshComponent? spline = null) : base(mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh.RenderData?.LODs, "Mesh has no LOD data");
        ArgumentNullException.ThrowIfNull(mesh.RenderData?.Bounds, "Mesh has no bounds");

        Bounds = mesh.RenderData.Bounds.GetBox();
        BodySetup = mesh.BodySetup;

        if (naniteFormat != ENaniteMeshFormat.NaniteOnly) // just so we don't waste time
        {
            ParseMeshRenderData(mesh.RenderData, quality, spline);
        }

        var shouldParseNanite = naniteFormat != ENaniteMeshFormat.NoNanite || LODs.Count == 0;
        if (shouldParseNanite && mesh.RenderData.NaniteResources is { PageStreamingStates.Length: > 0 } nanite)
        {
            ParseNaniteResources(nanite, naniteFormat);
        }
        else if (LODs.Count == 0) // in case someone put NaniteOnly but there was no nanite to parse
        {
            ParseMeshRenderData(mesh.RenderData, quality, spline);
        }

        SetLodSuffixes();
    }

    public StaticMeshDto(USplineMeshComponent spline, EMeshQuality quality = EMeshQuality.All) : this(spline.GetStaticMesh().Load<UStaticMesh>() ?? throw new ArgumentNullException(nameof(spline), "Spline mesh has no static mesh"), quality, ENaniteMeshFormat.NoNanite, spline)
    {

    }

    protected StaticMeshDto(ALandscapeProxy landscape) : base(landscape)
    {

    }

    private void ParseMeshRenderData(FStaticMeshRenderData renderData, EMeshQuality quality, USplineMeshComponent? spline = null)
    {
        foreach (var sourceLodIndex in quality.GetRange(renderData.LODs!.Length, i => renderData.LODs[i].SkipLod))
        {
            var screenSize = 0.0f;
            if (sourceLodIndex < renderData.ScreenSize.Length)
            {
                screenSize = renderData.ScreenSize[sourceLodIndex];
            }

            LODs.Add(MeshLodDto<MeshVertex>.FromStaticMesh(this, sourceLodIndex, renderData.LODs[sourceLodIndex], screenSize, spline));
        }
    }

    private void ParseNaniteResources(FNaniteResources nanite, ENaniteMeshFormat naniteFormat)
    {
        nanite.LoadAllPages();

        // Identify all high quality clusters
        var clusters = nanite.LoadedPages.Where(p => p != null)
            .SelectMany(p => p!.Clusters).Where(x => x.EdgeLength < 0.0f)
            .ToArray();

        // Check if we even have tris to parse.
        var numTris = 0;
        var numVerts = 0;
        var numUVs = 0u;
        var sectionCount = Materials.Length;
        foreach (var cluster in clusters)
        {
            numTris += cluster.TriIndices.Length;
            numVerts += cluster.Vertices.Length;
            numUVs = Math.Max(numUVs, cluster.NumUVs);

            // unfortunately we can't trust these indices
            if (!cluster.ShouldUseMaterialTable())
            {
                Clamp(ref cluster.Material0Index);
                Clamp(ref cluster.Material1Index);
                Clamp(ref cluster.Material2Index);
            }
            else for (var i = 0; i < cluster.MaterialRanges.Length; i++)
            {
                var index = cluster.MaterialRanges[i].MaterialIndex;
                Clamp(ref index);
                cluster.MaterialRanges[i] = new FMaterialRange(cluster.MaterialRanges[i], index);
            }
        }

        if (numTris > 0 && numVerts > 0)
        {
            var numTexCoords = nanite.Archive.Game >= EGame.GAME_UE5_6 ? (int) numUVs : nanite.NumInputTexCoords;
            var naniteLod = MeshLodDto<MeshVertex>.FromNaniteClusters(this, clusters, sectionCount, numTexCoords, numVerts);

            if (naniteFormat == ENaniteMeshFormat.NaniteFirst)
            {
                LODs.Insert(0, naniteLod);
            }
            else
            {
                LODs.Add(naniteLod); // covers: OnlyNaniteLOD, AllLayersNaniteLast, and the OnlyNormalLODs fallback
            }
        }

        // aggressively garbage collect since the asset is re-parsed every time by FModel
        // we don't need most of this data to still exist post mesh export anyway.
        // we also don't want that to json serialize anyway since 400mb+ json files are no fun.
        nanite.UnloadAllPages();
        GC.Collect();

        void Clamp(ref uint materialIndex)
        {
            materialIndex = Math.Clamp(materialIndex, 0, (uint) sectionCount - 1);
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        BodySetup = null;
    }
}

public class SkeletonDto : MeshDto<SkinnedMeshVertex>
{
    public readonly string? SkeletonName;
    public readonly MeshBoneDto[] Bones;

    public sealed override FBox Bounds { get; protected init; }
    public string? SkeletonPathName { get; private set; }
    public FVirtualBone[]? VirtualBones { get; private set; }

    protected SkeletonDto(USkeletalMesh mesh) : base(mesh)
    {
        Bounds = mesh.ImportedBounds.GetBox();

        var refSkeleton = mesh.ReferenceSkeleton;
        Bones = new MeshBoneDto[refSkeleton.FinalRefBonePose.Length];
        for (var i = 0; i < Bones.Length; i++)
        {
            Bones[i] = new MeshBoneDto(refSkeleton.FinalRefBoneInfo[i], refSkeleton.FinalRefBonePose[i]);
        }

        if (mesh.Skeleton.TryLoad<USkeleton>(out var skeleton))
        {
            SkeletonName = skeleton.Name;
            SkeletonPathName = skeleton.GetPathName();
            VirtualBones = skeleton.VirtualBones;
        }
    }

    public SkeletonDto(USkeleton skeleton) : base(skeleton)
    {
        var refSkeleton = skeleton.ReferenceSkeleton;
        Bones = new MeshBoneDto[refSkeleton.FinalRefBonePose.Length];
        for (var i = 0; i < Bones.Length; i++)
        {
            var bone = new MeshBoneDto(refSkeleton.FinalRefBoneInfo[i], refSkeleton.FinalRefBonePose[i]);
            Bounds = Bounds.ExpandBy(bone.Transform.Translation);
            Bones[i] = bone;
        }

        SkeletonName = skeleton.Name;
        SkeletonPathName = skeleton.GetPathName();
        VirtualBones = skeleton.VirtualBones;
    }

    public override void Dispose()
    {
        base.Dispose();

        Array.Clear(Bones);
        SkeletonPathName = null;
        VirtualBones = null;
    }
}

public sealed class SkeletalMeshDto : SkeletonDto
{
    public FPackageIndex? PhysicsAsset { get; private set; }
    public FPackageIndex[]? MorphTargets { get; private set; }
    public FPackageIndex[]? AssetUserData { get; private set; }

    public SkeletalMeshDto(USkeletalMesh mesh, EMeshQuality quality = EMeshQuality.All, ENaniteMeshFormat naniteFormat = ENaniteMeshFormat.NoNanite) : base(mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh.LODModels, "Mesh has no LOD data");

        PhysicsAsset = mesh.PhysicsAsset;
        MorphTargets = mesh.MorphTargets;
        AssetUserData = mesh.AssetUserData;

        // TODO: nanite skel mesh: FarFarWest/Content/Characters/Characters/Goat/SKM_Goat.uasset
        // if (mesh.NaniteResources is { PageStreamingStates.Length: > 0 } nanite)
        // {
        //
        // }

        foreach (var sourceLodIndex in quality.GetRange(mesh.LODModels.Length, i => mesh.LODModels[i].SkipLod))
        {
            LODs.Add(MeshLodDto<SkinnedMeshVertex>.FromSkeletalMesh(this, sourceLodIndex, mesh.LODModels[sourceLodIndex], mesh.LODInfo[sourceLodIndex].ScreenSize.Value));
        }

        SetLodSuffixes();
    }

    public override void Dispose()
    {
        base.Dispose();

        PhysicsAsset = null;
        MorphTargets = null;
        AssetUserData = null;
    }
}

public sealed class LandscapeMeshDto : StaticMeshDto
{
    public readonly ConcurrentDictionary<string, SKBitmap>? BitmapTextures;
    public readonly Image<L16>? HeightmapTexture;

    public LandscapeMeshDto(ALandscapeProxy landscape, ELandscapeFlags flags = ELandscapeFlags.Mesh, ULandscapeComponent[]? components = null) : base(landscape)
    {
        var sizeQuads = landscape.ComponentSizeQuads;

        if (components == null)
        {
            components = new ULandscapeComponent[landscape.LandscapeComponents.Length];
            for (var i = 0; i < components.Length; i++)
            {
                components[i] = landscape.LandscapeComponents[i].Load<ULandscapeComponent>() ?? throw new ArgumentNullException($"Failed to load landscape component at index {i}");
                if (sizeQuads == -1)
                {
                    sizeQuads = components[i].ComponentSizeQuads;
                }
                else if (sizeQuads != components[i].ComponentSizeQuads)
                {
                    throw new InvalidOperationException($"Inconsistent component sizes in landscape. Expected {sizeQuads}, but got {components[i].ComponentSizeQuads} at index {i}");
                }
            }
        }

        foreach (var component in components)
        {
            Bounds = Bounds.ExpandBy(component.CachedLocalBox.GetSize());
        }

        LODs.Add(MeshLodDto<MeshVertex>.FromLandscapeMesh(this, components, sizeQuads, flags, out BitmapTextures, out HeightmapTexture));
    }

    public LandscapeMeshDto(ULandscapeComponent component) : base(component)
    {
        Bounds = component.CachedLocalBox;
        LODs.Add(MeshLodDto<MeshVertex>.FromLandscapeMesh(this, [component], component.ComponentSizeQuads, ELandscapeFlags.Mesh, out BitmapTextures, out HeightmapTexture));
    }

    public override void Dispose()
    {
        base.Dispose();

        BitmapTextures?.Clear();
    }
}
