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

public abstract class Mesh<TVertex> where TVertex : MeshVertex, new()
{
    public readonly List<MeshLod<TVertex>> LODs = [];
    public readonly MeshMaterial[] Materials = [];
    public readonly FPackageIndex[]? Sockets;

    public abstract FBox Bounds { get; protected init; }

    protected Mesh(UStaticMesh mesh)
    {
        Materials = new MeshMaterial[mesh.StaticMaterials.Length];
        for (var i = 0; i < Materials.Length; i++)
        {
            Materials[i] = new MeshMaterial(mesh.StaticMaterials[i]);
        }
        Sockets = mesh.Sockets;
    }

    protected Mesh(USkeletalMesh mesh)
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

    protected Mesh(USkeleton skeleton)
    {
        Sockets = skeleton.Sockets;
    }

    protected Mesh(ALandscapeProxy landscape)
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
}

public class StaticMesh : Mesh<MeshVertex>
{
    public sealed override FBox Bounds { get; protected init; }
    public readonly FPackageIndex? BodySetup;

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
}

public class Skeleton : Mesh<SkinnedMeshVertex>
{
    public sealed override FBox Bounds { get; protected init; }
    public readonly List<MeshBone> RefSkeleton = [];
    public readonly string? SkeletonPathName;
    public readonly FVirtualBone[]? VirtualBones;

    protected Skeleton(USkeletalMesh mesh) : base(mesh)
    {
        Bounds = mesh.ImportedBounds.GetBox();

        var refSkeleton = mesh.ReferenceSkeleton;
        for (var i = 0; i < refSkeleton.FinalRefBonePose.Length; i++)
        {
            RefSkeleton.Add(new MeshBone(refSkeleton.FinalRefBoneInfo[i], refSkeleton.FinalRefBonePose[i]));
        }

        if (mesh.Skeleton.TryLoad<USkeleton>(out var skeleton))
        {
            SkeletonPathName = skeleton.GetPathName();
            VirtualBones = skeleton.VirtualBones;
        }
    }

    public Skeleton(USkeleton skeleton) : base(skeleton)
    {
        var refSkeleton = skeleton.ReferenceSkeleton;
        for (var i = 0; i < refSkeleton.FinalRefBonePose.Length; i++)
        {
            var bone = new MeshBone(refSkeleton.FinalRefBoneInfo[i], refSkeleton.FinalRefBonePose[i]);
            Bounds = Bounds.ExpandBy(bone.Transform.Translation);
            RefSkeleton.Add(bone);
        }

        SkeletonPathName = skeleton.GetPathName();
        VirtualBones = skeleton.VirtualBones;
    }
}

public sealed class SkeletalMesh : Skeleton
{
    public readonly FPackageIndex? PhysicsAsset;
    public readonly FPackageIndex[]? MorphTargets;
    public readonly FPackageIndex[]? AssetUserData;

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
}

public sealed class LandscapeMesh : StaticMesh
{
    public readonly ConcurrentDictionary<string, SKBitmap>? WeightmapTextures;
    public readonly SKBitmap? NormalTexture;
    public readonly Image<L16>? HeightmapTexture;

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
}
