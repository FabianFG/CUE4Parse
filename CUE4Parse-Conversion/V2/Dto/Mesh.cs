using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse_Conversion.Landscape;
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

namespace CUE4Parse_Conversion.V2.Dto;

public abstract class Mesh<TVertex> : ObjectDto where TVertex : struct, IMeshVertex
{
    public readonly IList<MeshLod<TVertex>> LODs = [];
    public readonly MeshMaterial[] Materials = [];

    public FPackageIndex[]? Sockets { get; private set; }
    public abstract FBox Bounds { get; protected init; }

    protected Mesh(UStaticMesh mesh) : base(mesh)
    {
        Materials = new MeshMaterial[mesh.StaticMaterials.Length];
        for (var i = 0; i < Materials.Length; i++)
        {
            Materials[i] = new MeshMaterial(mesh.StaticMaterials[i]);
        }
        Sockets = mesh.Sockets;
    }

    protected Mesh(USkeletalMesh mesh) : base(mesh)
    {
        Materials = new MeshMaterial[mesh.SkeletalMaterials.Length];
        for (var i = 0; i < Materials.Length; i++)
        {
            Materials[i] = new MeshMaterial(mesh.SkeletalMaterials[i]);
        }

        var sockets = new List<FPackageIndex>(mesh.Sockets);
        if (mesh.Skeleton.TryLoad<USkeleton>(out var skeleton))
        {
            sockets.AddRange(skeleton.Sockets);
        }
        Sockets = sockets.ToArray();
    }

    protected Mesh(USkeleton skeleton) : base(skeleton)
    {
        Sockets = skeleton.Sockets;
    }

    protected Mesh(ALandscapeProxy landscape) : base(landscape)
    {
        Materials = [new MeshMaterial(null, landscape.LandscapeMaterial)];
    }

    public MeshMaterial? GetMaterial(MeshSection section)
    {
        var index = section.MaterialIndex;
        if (index < 0 || index >= Materials.Length)
        {
            return null;
        }

        return Materials[index];
    }

    public override void Dispose()
    {
        LODs.Clear();
        Array.Clear(Materials);
        Sockets = null;
    }
}

public class StaticMesh : Mesh<MeshVertex>
{
    public FPackageIndex? BodySetup { get; private set; }
    public sealed override FBox Bounds { get; protected init; }

    public StaticMesh(UStaticMesh mesh, ENaniteMeshFormat naniteFormat = ENaniteMeshFormat.OnlyNormalLODs, USplineMeshComponent? spline = null) : base(mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh.RenderData?.LODs, "Mesh has no LOD data");
        ArgumentNullException.ThrowIfNull(mesh.RenderData?.Bounds, "Mesh has no bounds");

        Bounds = mesh.RenderData.Bounds.GetBox();
        BodySetup = mesh.BodySetup;

        if (naniteFormat != ENaniteMeshFormat.OnlyNaniteLOD)
        {
            for (var i = 0; i < mesh.RenderData.LODs.Length; i++)
            {
                if (mesh.RenderData.LODs[i].SkipLod) continue;

                var screenSize = 0.0f;
                if (i < mesh.RenderData.ScreenSize.Length)
                {
                    screenSize = mesh.RenderData.ScreenSize[i];
                }

                LODs.Add(MeshLod<MeshVertex>.FromStaticMesh(this, mesh.RenderData.LODs[i], screenSize, spline));
            }
        }

        var shouldParseNanite = naniteFormat != ENaniteMeshFormat.OnlyNormalLODs || LODs.Count == 0;
        if (shouldParseNanite && mesh.RenderData.NaniteResources is { PageStreamingStates.Length: > 0 } nanite)
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
            foreach (var cluster in clusters)
            {
                numTris += cluster.TriIndices.Length;
                numVerts += cluster.Vertices.Length;
                numUVs = Math.Max(numUVs, cluster.NumUVs);
            }

            if (numTris > 0 && numVerts > 0)
            {
                var numTexCoords = nanite.Archive.Game >= EGame.GAME_UE5_6 ? (int) numUVs : nanite.NumInputTexCoords;
                var naniteLod = MeshLod<MeshVertex>.FromNaniteClusters(this, clusters, Materials.Length, numTexCoords, numVerts);

                if (naniteFormat == ENaniteMeshFormat.AllLayersNaniteFirst)
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
        }
    }

    public StaticMesh(USplineMeshComponent spline) : this(spline.GetStaticMesh().Load<UStaticMesh>() ?? throw new ArgumentNullException(nameof(spline), "Spline mesh has no static mesh"), ENaniteMeshFormat.OnlyNormalLODs, spline)
    {

    }

    protected StaticMesh(ALandscapeProxy landscape) : base(landscape)
    {

    }

    public override void Dispose()
    {
        base.Dispose();

        BodySetup = null;
    }
}

public class Skeleton : Mesh<SkinnedMeshVertex>
{
    public readonly string? SkeletonName;
    public readonly MeshBone[] Bones;

    public sealed override FBox Bounds { get; protected init; }
    public string? SkeletonPathName { get; private set; }
    public FVirtualBone[]? VirtualBones { get; private set; }

    protected Skeleton(USkeletalMesh mesh) : base(mesh)
    {
        Bounds = mesh.ImportedBounds.GetBox();

        var refSkeleton = mesh.ReferenceSkeleton;
        Bones = new MeshBone[refSkeleton.FinalRefBonePose.Length];
        for (var i = 0; i < Bones.Length; i++)
        {
            Bones[i] = new MeshBone(refSkeleton.FinalRefBoneInfo[i], refSkeleton.FinalRefBonePose[i]);
        }

        if (mesh.Skeleton.TryLoad<USkeleton>(out var skeleton))
        {
            SkeletonName = skeleton.Name;
            SkeletonPathName = skeleton.GetPathName();
            VirtualBones = skeleton.VirtualBones;
        }
    }

    public Skeleton(USkeleton skeleton) : base(skeleton)
    {
        var refSkeleton = skeleton.ReferenceSkeleton;
        Bones = new MeshBone[refSkeleton.FinalRefBonePose.Length];
        for (var i = 0; i < Bones.Length; i++)
        {
            var bone = new MeshBone(refSkeleton.FinalRefBoneInfo[i], refSkeleton.FinalRefBonePose[i]);
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

public sealed class SkeletalMesh : Skeleton
{
    public FPackageIndex? PhysicsAsset { get; private set; }
    public FPackageIndex[]? MorphTargets { get; private set; }
    public FPackageIndex[]? AssetUserData { get; private set; }

    public SkeletalMesh(USkeletalMesh mesh) : base(mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh.LODModels, "Mesh has no LOD data");

        PhysicsAsset = mesh.PhysicsAsset;
        MorphTargets = mesh.MorphTargets;
        AssetUserData = mesh.AssetUserData;

        for (var i = 0; i < mesh.LODModels.Length; i++)
        {
            if (mesh.LODModels[i].SkipLod) continue;
            LODs.Add(MeshLod<SkinnedMeshVertex>.FromSkeletalMesh(this, mesh.LODModels[i], mesh.LODInfo[i].ScreenSize.Value));
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

public sealed class LandscapeMesh : StaticMesh
{
    public ConcurrentDictionary<string, SKBitmap>? WeightmapTextures { get; private set; }
    public SKBitmap? NormalTexture { get; private set; }
    public Image<L16>? HeightmapTexture { get; private set; }

    public LandscapeMesh(ALandscapeProxy landscape, ELandscapeExportFlags flags = ELandscapeExportFlags.Mesh, ULandscapeComponent[]? components = null) : base(landscape)
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

        LODs.Add(MeshLod<MeshVertex>.FromLandscapeMesh(this, components, sizeQuads, NormalTexture, HeightmapTexture));
    }

    public override void Dispose()
    {
        base.Dispose();

        WeightmapTextures?.Clear();
        WeightmapTextures = null;
        NormalTexture = null;
        HeightmapTexture = null;
    }
}
