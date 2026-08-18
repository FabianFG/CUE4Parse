using System.Collections.Concurrent;
using CUE4Parse_Conversion.Options;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;
using CUE4Parse.UE4.Assets.Exports.GeometryCollection;
using CUE4Parse.UE4.Assets.Exports.Nanite;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Chaos.GeometryCollection;
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

    protected MeshDto(UObject owner, MeshMaterialDto[] materials) : base(owner)
    {
        Materials = materials;
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

    protected MeshDto(UGeometryCollection mesh) : base(mesh)
    {
        Materials = new MeshMaterialDto[mesh.Materials.Length];
        for (var i = 0; i < Materials.Length; i++)
        {
            Materials[i] = new MeshMaterialDto($"{i}", mesh.Materials[i]);
        }
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

    public MeshMaterialDto? GetMaterial(MeshSectionDto section)
    {
        var index = section.MaterialIndex;
        if (index < 0 || index >= Materials.Length)
        {
            return null;
        }

        return Materials[index];
    }

    protected void ParseNaniteResources<TNaniteVertex>(MeshDto<TNaniteVertex> owner, FNaniteResources nanite, ENaniteMeshFormat naniteFormat) where TNaniteVertex : struct, IMeshVertex, INaniteVertex<TNaniteVertex>
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
        var sectionCount = owner.Materials.Length;
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
            var naniteLod = MeshLodDto<TNaniteVertex>.FromNaniteClusters(owner, clusters, sectionCount, numTexCoords, numVerts);

            if (naniteFormat == ENaniteMeshFormat.NaniteFirst)
            {
                owner.LODs.Insert(0, naniteLod);
            }
            else
            {
                owner.LODs.Add(naniteLod); // covers: OnlyNaniteLOD, AllLayersNaniteLast, and the OnlyNormalLODs fallback
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

    protected StaticMeshDto(UObject owner, MeshMaterialDto[] materials) : base(owner, materials)
    {

    }

    /// <summary>
    /// Builds a static mesh DTO purely from nanite cluster data
    /// </summary>
    public StaticMeshDto(UObject owner, MeshMaterialDto[] materials, FNaniteResources nanite, ENaniteMeshFormat naniteFormat = ENaniteMeshFormat.NaniteOnly) : base(owner, materials)
    {
        if (nanite.PageStreamingStates.Length > 0)
        {
            ParseNaniteResources(this, nanite, naniteFormat);
        }

        Bounds = LODs.First().CalculateLodBounds();

        SetLodSuffixes();
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
            ParseNaniteResources(this, nanite, naniteFormat);
        }
        else if (LODs.Count == 0) // in case someone put NaniteOnly but there was no nanite to parse
        {
            ParseMeshRenderData(mesh.RenderData, quality, spline);
        }

        SetLodSuffixes();
    }

    public StaticMeshDto(UGeometryCollection mesh, ENaniteMeshFormat naniteFormat = ENaniteMeshFormat.NoNanite) : base(mesh)
    {
        FBox? bounds = null;
        if (mesh.RenderData?.PreSkinnedBounds is { } preSkinnedBounds)
            bounds = preSkinnedBounds.GetBox();
        else if (mesh.RenderData?.MeshDescription?.PreSkinnedBounds is { } meshDescriptionBounds)
            bounds = meshDescriptionBounds.GetBox();

        if (naniteFormat != ENaniteMeshFormat.NaniteOnly) // just so we don't waste time
        {
            ParseCollectionData(mesh.RenderData, mesh.GeometryCollection);
        }

        var shouldParseNanite = naniteFormat != ENaniteMeshFormat.NoNanite || LODs.Count == 0;
        if (shouldParseNanite && mesh.RenderData?.NaniteResources is { PageStreamingStates.Length: > 0 } nanite)
        {
            ParseNaniteResources(this, nanite, naniteFormat);

            if (nanite.MeshBounds is { } meshBounds)
                bounds = meshBounds.GetBox();
        }
        else if (LODs.Count == 0) // in case someone put NaniteOnly but there was no nanite to parse
        {
            ParseCollectionData(mesh.RenderData, mesh.GeometryCollection);
        }

        bounds ??= LODs.FirstOrDefault()?.CalculateLodBounds();
        Bounds = bounds ?? new FBox(FVector.ZeroVector, FVector.OneVector);

        SetLodSuffixes();
    }

    public StaticMeshDto(USplineMeshComponent spline, EMeshQuality quality = EMeshQuality.All) : this(spline.GetStaticMesh().Load<UStaticMesh>() ?? throw new ArgumentNullException(nameof(spline), "Spline mesh has no static mesh"), quality, ENaniteMeshFormat.NoNanite, spline)
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

    private void ParseCollectionData(FGeometryCollectionRenderData? renderData, FGeometryCollection? collection)
    {
        if (renderData?.bHasMeshData == false) return; // don't crash, keep LODs to 0, so it tries the nanite data

        var resources = renderData?.MeshResources;
        var description = renderData?.MeshDescription;
        if (renderData?.CustomData is List<(FGeometryCollectionMeshResources?, FGeometryCollectionMeshDescription?)> { Count: > 0 } customData) // MR
        {
            resources = customData[0].Item1;
            description = customData[0].Item2;
            // CustomData[0] = SM
            // CustomData[1] = plane
            // CustomData[2] = SK?? it really looks like it's CustomData[0] with all bones at the origin
            // CustomData[3] = plane
        }

        if (resources != null && description != null)
        {
            LODs.Add(MeshLodDto<SkinnedMeshVertex>.FromRenderData(this, 0u, resources, description.Value, collection));
            return;
        }

        if (collection != null &&
            collection.GroupInfo.TryGetValue("Vertices", out var vertices) && vertices.Size > 0 &&
            collection.GroupInfo.TryGetValue("Faces", out var faces) && faces.Size > 0 &&
            collection.GroupInfo.TryGetValue("Material", out var material) && material.Size > 0)
        {
            LODs.Add(MeshLodDto<SkinnedMeshVertex>.FromArrayCollection(this, 0u, collection));
            return;
        }

        throw new InvalidOperationException("Geometry collection has no render data or vertex data");
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

        var componentSpace = new FTransform[Bones.Length];
        for (var i = 0; i < Bones.Length; i++)
        {
            var bone = new MeshBoneDto(refSkeleton.FinalRefBoneInfo[i], refSkeleton.FinalRefBonePose[i]);
            componentSpace[i] = bone.ParentIndex >= 0 ? bone.Transform * componentSpace[bone.ParentIndex] : bone.Transform;
            Bounds += componentSpace[i].Translation;
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

    public SkeletalMeshDto(USkeletalMesh mesh, EMeshQuality quality = EMeshQuality.All, ENaniteMeshFormat naniteFormat = ENaniteMeshFormat.NoNanite, bool exportMorphTarget = true) : base(mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh.LODModels, "Mesh has no LOD data");

        PhysicsAsset = mesh.PhysicsAsset;
        MorphTargets = mesh.MorphTargets;
        AssetUserData = mesh.AssetUserData;

        if (naniteFormat != ENaniteMeshFormat.NaniteOnly) // just so we don't waste time
        {
            ParseMeshRenderData(mesh, quality);
        }

        var shouldParseNanite = naniteFormat != ENaniteMeshFormat.NoNanite && MorphTargets is { Length: > 0} && exportMorphTarget || LODs.Count == 0;
        if (shouldParseNanite && mesh.NaniteResources is { PageStreamingStates.Length: > 0 } nanite)
        {
            ParseNaniteResources(this, nanite, naniteFormat);
        }
        else if (LODs.Count == 0) // in case someone put NaniteOnly but there was no nanite to parse
        {
            ParseMeshRenderData(mesh, quality);
        }

        SetLodSuffixes();
    }

    private void ParseMeshRenderData(USkeletalMesh mesh, EMeshQuality quality)
    {
        foreach (var sourceLodIndex in quality.GetRange(mesh.LODModels!.Length, i => mesh.LODModels[i].SkipLod))
        {
            LODs.Add(MeshLodDto<SkinnedMeshVertex>.FromSkeletalMesh(this, sourceLodIndex, mesh.LODModels[sourceLodIndex], mesh.LODInfo[sourceLodIndex].ScreenSize.Value));
        }
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

    public LandscapeMeshDto(ALandscapeProxy landscape, ELandscapeFlags flags = ELandscapeFlags.Mesh, ULandscapeComponent[]? components = null)
        : this(landscape, flags, PrepareComponents(landscape, components))
    {

    }

    private LandscapeMeshDto(ALandscapeProxy landscape, ELandscapeFlags flags, (ULandscapeComponent[] Components, int SizeQuads, MeshMaterialDto[] Materials) prepared)
        : base(landscape, prepared.Materials)
    {
        foreach (var component in prepared.Components)
        {
            Bounds = Bounds.ExpandBy(component.CachedLocalBox.GetSize());
        }

        LODs.Add(MeshLodDto<MeshVertex>.FromLandscapeMesh(this, prepared.Components, prepared.SizeQuads, flags, out BitmapTextures, out HeightmapTexture));
    }

    public LandscapeMeshDto(ULandscapeComponent component)
        : base(component, [new MeshMaterialDto(component.OverrideMaterial?.Name, component.OverrideMaterial)])
    {
        Bounds = component.CachedLocalBox;
        LODs.Add(MeshLodDto<MeshVertex>.FromLandscapeMesh(this, [component], component.ComponentSizeQuads, ELandscapeFlags.Mesh, out BitmapTextures, out HeightmapTexture));
    }

    private static (ULandscapeComponent[] Components, int SizeQuads, MeshMaterialDto[] Materials) PrepareComponents(ALandscapeProxy landscape, ULandscapeComponent[]? components)
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

        var materials = new MeshMaterialDto[components.Length];
        for (var i = 0; i < components.Length; i++)
        {
            var mat = components[i].OverrideMaterial ?? landscape.LandscapeMaterial;
            materials[i] = new MeshMaterialDto(mat?.Name, mat);
        }

        return (components, sizeQuads, materials);
    }

    public override void Dispose()
    {
        base.Dispose();

        BitmapTextures?.Clear();
    }
}
