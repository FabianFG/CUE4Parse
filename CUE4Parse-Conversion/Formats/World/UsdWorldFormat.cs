using System;
using System.Linq;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Writers.USD;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.Formats.World;

public class UsdWorldFormat : IWorldExportFormat
{
    public string DisplayName => "USD World (.usda)";

    public ExportFile Build(WorldDto dto, WorldAssetPaths paths)
    {
        var worldPrim = UsdPrim.Def("Scope", dto.Name);
        foreach (var actor in dto.Actors)
        {
            worldPrim.Add(BuildActorPrim(actor, paths));
        }

        var stage = new UsdStage(worldPrim);
        if (paths.SubLayers is { Count: > 0 })
        {
            stage.AddMetadata("subLayers", UsdValue.Array(paths.SubLayers.Select(UsdValue.AssetPath)));
        }

        return new ExportFile("usda", stage.SerializeToBinary());
    }

    private UsdPrim BuildActorPrim(ActorDto actor, WorldAssetPaths paths)
    {
        var actorPrim = UsdPrim.Def("Scope", actor.Name);
        if (!actor.IsVisible)
        {
            actorPrim.AddPrimvar("token", "visibility", UsdValue.Token("invisible"));
        }

        if (actor.RootComponent is { } root)
        {
            var rootPrim = BuildComponentPrim(root, paths);
            BuildChildrenComponent(root, rootPrim, paths);

            if (actor.AdditionalWorlds is { Count: > 0 } additionalWorlds)
            {
                foreach (var world in additionalWorlds)
                {
                    if (!paths.Worlds.TryGetValue(world.Name, out var worldPath)) continue;
                    var worldRef = UsdPrim.Def("Xform", world.Name);
                    worldRef.SetReference(new UsdReferenceList([new UsdReference(worldPath)]));
                    rootPrim.Add(worldRef);
                }
            }

            actorPrim.Add(rootPrim);
        }

        return actorPrim;
    }

    private UsdPrim BuildComponentPrim(SceneComponentDto component, WorldAssetPaths paths)
    {
        var transform = component.Transform;
        UsdPrim prim;

        switch (component)
        {
            case InstancedStaticMeshComponentDto ism:
                if (ism.Transforms.Length > 0) prim = BuildPointInstancer(ism, paths);
                else goto default; // break the inheritance chain, we don't want to show a mesh if there are no instances
                break;
            case SplineMeshComponentDto spline when paths.SplineMeshes.TryGetValue(spline, out var splinePath):
                prim = ReferenceMesh("Mesh", component.Name, splinePath, spline.OverrideMaterials, paths);
                break;
            case SkinnedMeshComponentDto skinned when paths.TryGet(skinned.MeshPtr, out var skelPath):
                prim = ReferenceSkinnedMesh(skinned, skelPath, paths);
                break;
            case MeshComponentDto mesh when paths.TryGet(mesh.MeshPtr, out var meshPath):
                prim = ReferenceMesh("Mesh", component.Name, meshPath, mesh.OverrideMaterials, paths);
                break;
            case MeshComponentDto:
                prim = CreateDummyCube(component.Name);
                break;
            case LandscapeMeshComponentDto landscape when LandscapeMeshComponentDto.PerComponentExport && paths.LandscapeMeshes.TryGetValue(landscape, out var landscapePath):
                transform.Translation = FVector.ZeroVector; // the exporter offsets the mesh by SectionBaseX/Y
                prim = ReferencePrim("Mesh", component.Name, landscapePath);
                break;
            case LightComponentBaseDto light:
                transform = transform.WithLightOrientationCorrection(-MathF.PI / 2f); // don't ask me why
                prim = light.ToLightPrim();
                break;
            case BrushComponentDto brush:
                prim = brush.ToMeshPrim();
                break;
            case ShapeComponentDto shape:
                prim = shape.ToShapePrim();
                break;
            default:
                prim = UsdPrim.Def("Xform", component.Name);
                break;
        }

        if (component is SkeletalMeshComponentDto { AnimationData: { } animationData })
        {
            // TODO
        }

        if (component is PrimitiveComponentDto { IsVisible: false })
        {
            prim.AddPrimvar("token", "visibility", UsdValue.Token("invisible"));
        }
        prim.Add(transform.ToTransformAttributes());
        return prim;
    }

    private UsdPrim ReferencePrim(string typeName, string name, string path)
    {
        var prim = UsdPrim.Def(typeName, name);
        prim.SetReference(new UsdReferenceList([new UsdReference(path)]));
        return prim;
    }

    private UsdPrim ReferenceMesh(string typeName, string name, string path, FPackageIndex?[] overrides, WorldAssetPaths paths)
    {
        var prim = ReferencePrim(typeName, name, path);
        if (overrides is { Length: > 0 })
        {
            ApplyMaterialOverrides(prim, overrides, paths);
        }
        return prim;
    }

    private UsdPrim ReferenceSkinnedMesh(SkinnedMeshComponentDto skinned, string path, WorldAssetPaths paths)
    {
        var prim = ReferencePrim("SkelRoot", skinned.Name, path);
        if (skinned.OverrideMaterials is { Length: > 0 } overrides)
        {
            var meshOver = UsdPrim.Over("Mesh", skinned.MeshPtr.Name);
            ApplyMaterialOverrides(meshOver, overrides, paths);
            prim.Add(meshOver);
        }
        return prim;
    }

    private void ApplyMaterialOverrides(UsdPrim meshPrim, FPackageIndex?[] overrides, WorldAssetPaths paths)
    {
        var materialsScope = UsdPrim.Def("Scope", "OverrideMaterials");
        meshPrim.Add(materialsScope);

        for (var i = 0; i < overrides.Length; i++)
        {
            var mat = overrides[i];
            if (mat is null || mat.IsNull) continue;

            var matPrim = UsdPrim.Def("Material", mat.Name);
            if (paths.TryGet(mat, out var matPath))
            {
                matPrim.SetReference(new UsdReferenceList([new UsdReference(matPath)]));
            }

            materialsScope.Add(matPrim);

            var sectionOver = UsdPrim.Over("GeomSubset", $"Section_{i}");
            sectionOver.AddMetadata("prepend apiSchemas", UsdValue.Array(UsdValue.Token("MaterialBindingAPI")));
            sectionOver.Add(new UsdRelationship("material:binding", matPrim));
            meshPrim.Add(sectionOver);
        }
    }

    private void BuildChildrenComponent(SceneComponentDto component, UsdPrim parentPrim, WorldAssetPaths paths)
    {
        foreach (var child in component.Children)
        {
            // a different actor is attached to this component, write it down
            if (child.Owner != component.Owner && child.Owner.RootComponent == child)
            {
                parentPrim.Add(BuildActorPrim(child.Owner, paths));
                continue;
            }

            var childPrim = BuildComponentPrim(child, paths);
            BuildChildrenComponent(child, childPrim, paths);
            parentPrim.Add(childPrim);
        }
    }

    private UsdPrim BuildPointInstancer(InstancedStaticMeshComponentDto ism, WorldAssetPaths paths)
    {
        var instancer = UsdPrim.Def("PointInstancer", "Instances");
        var prototypes = UsdPrim.Def("Scope", "Prototypes");

        // Build the prototype prim
        UsdPrim prototypePrim;
        if (paths.TryGet(ism.MeshPtr, out var meshPath))
        {
            prototypePrim = ReferenceMesh("Mesh", ism.Name, meshPath, ism.OverrideMaterials, paths);
        }
        else
        {
            prototypePrim = CreateDummyCube(ism.Name);
        }

        prototypes.Add(prototypePrim);
        instancer.Add(prototypes); // must add before GetPath() is called on prototypePrim

        instancer.Add(new UsdRelationship("prototypes", prototypePrim));

        // Per-instance arrays – coordinate mirror matches the rest of the pipeline
        var count = ism.Transforms.Length;
        var protoIndices = new UsdValue[count];
        var positions = new UsdValue[count];
        var orientations = new UsdValue[count];
        var scales = new UsdValue[count];

        for (var i = 0; i < count; i++)
        {
            var t = ism.Transforms[i].Translation;
            var r = ism.Transforms[i].Rotation;
            var s = ism.Transforms[i].Scale3D;

            protoIndices[i] = UsdValue.Int(0);
            positions[i] = UsdValue.Tuple(t.X, -t.Y, t.Z); // MIRROR_MESH
            orientations[i] = UsdValue.Tuple(r.W, -r.X, r.Y, -r.Z); // quath (w,x,y,z) MIRROR_MESH
            scales[i] = UsdValue.Tuple(s.X, s.Y, s.Z);
        }

        instancer.Add(new UsdAttribute("int[]", "protoIndices", UsdValue.Array(protoIndices)));
        instancer.Add(new UsdAttribute("point3f[]", "positions", UsdValue.Array(positions)));
        instancer.Add(new UsdAttribute("quath[]", "orientations", UsdValue.Array(orientations)));
        instancer.Add(new UsdAttribute("float3[]", "scales", UsdValue.Array(scales)));

        return instancer;
    }

    private UsdPrim CreateDummyCube(string name = "Cube")
    {
        var mesh = UsdPrim.Def("Cube", name);
        return mesh;
    }
}
