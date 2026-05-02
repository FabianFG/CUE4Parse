using System.Collections.Generic;
using System.Linq;
using CUE4Parse_Conversion.USD;
using CUE4Parse_Conversion.V2.Dto.World;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.V2.Formats.World;

public class UsdWorldFormat : IWorldExportFormat
{
    public string DisplayName => "USD World (.usda)";

    public ExportFile Build(WorldDto dto, IReadOnlyDictionary<FPackageIndex, string>? meshes = null, IReadOnlyList<string>? subLayers = null, IReadOnlyDictionary<string, string>? worlds = null)
    {
        var lookup = new MeshLookup(meshes);

        var worldPrim = UsdPrim.Def("Scope", dto.Name);
        foreach (var actor in dto.Actors)
        {
            worldPrim.Add(BuildActorPrim(actor, lookup, worlds));
        }

        var stage = new UsdStage(worldPrim);
        if (subLayers is { Count: > 0 })
        {
            stage.AddMetadata("subLayers", UsdValue.Array(subLayers.Select(UsdValue.AssetPath)));
        }

        return new ExportFile("usda", stage.SerializeToBinary());
    }

    private UsdPrim BuildActorPrim(ActorDto actor, MeshLookup lookup, IReadOnlyDictionary<string, string>? worldPaths)
    {
        var actorPrim = UsdPrim.Def("Scope", actor.Name);
        if (!actor.IsVisible)
        {
            actorPrim.AddPrimvar("token", "visibility", UsdValue.Token("invisible"));
        }

        if (actor.RootComponent is { } root)
        {
            var rootPrim = BuildComponentPrim(root, lookup);
            BuildChildrenComponent(root, rootPrim, lookup, worldPaths);

            if (actor.AdditionalWorlds is { Count: > 0 } additionalWorlds && worldPaths is not null)
            {
                foreach (var world in additionalWorlds)
                {
                    if (!worldPaths.TryGetValue(world.Name, out var worldPath)) continue;
                    var worldRef = UsdPrim.Def("Xform", world.Name);
                    worldRef.SetReference(new UsdReferenceList([new UsdReference(worldPath)]));
                    rootPrim.Add(worldRef);
                }
            }

            actorPrim.Add(rootPrim);
        }

        foreach (var child in actor.ChildActors)
        {
            actorPrim.Add(BuildActorPrim(child, lookup, worldPaths));
        }

        return actorPrim;
    }

    private UsdPrim BuildComponentPrim(SceneComponentDto component, MeshLookup lookup)
    {
        var prim = UsdPrim.Def("Xform", component.Name);

        if (component is PrimitiveComponentDto { IsVisible: false })
        {
            prim.AddPrimvar("token", "visibility", UsdValue.Token("invisible"));
        }

        var transform = component.Transform;
        switch (component)
        {
            case InstancedStaticMeshComponentDto { Transforms.Length: > 0 } ism:
                prim.Add(BuildPointInstancer(ism, lookup));
                break;
            case MeshComponentDto mesh when lookup.TryGet(mesh.MeshPtr, out var path):
                ApplyMaterialOverrides(prim, mesh, mesh.MeshPtr.Name);
                prim.SetReference(new UsdReferenceList([new UsdReference(path)]));
                break;
            case MeshComponentDto mesh:
                prim.Add(CreateDummyCube(mesh.MeshPtr.Name));
                break;
            case LandscapeMeshComponentDto landscape when LandscapeMeshComponentDto.PerComponentExport:
                transform.Translation = FVector.ZeroVector; // the exporter is gonna offset the mesh by SectionBaseX/Y
                prim.SetReference(new UsdReferenceList([new UsdReference(landscape.Ref)]));
                break;
            case BrushComponentDto brush:
                prim.Add(brush.ToMeshPrim());
                break;
            case ShapeComponentDto shape:
                prim.Add(shape.ToShapePrim());
                break;
        }

        prim.Add(transform.ToTransformAttributes());
        return prim;
    }

    private void ApplyMaterialOverrides(UsdPrim componentPrim, MeshComponentDto component, string meshAssetName)
    {
        if (component.OverrideMaterials is not { Length: > 0 } overrides) return;

        var materialsScope = UsdPrim.Def("Scope", "OverrideMaterials");
        componentPrim.Add(materialsScope);

        var meshOver = UsdPrim.Over("Mesh", meshAssetName);
        componentPrim.Add(meshOver);

        for (var i = 0; i < overrides.Length; i++)
        {
            var mat = overrides[i];
            if (mat is null || mat.IsNull) continue;

            var matPrim = UsdPrim.Def("Material", mat.Name);
            // if (define)
            // {
            //     // TODO: define the prim
            // }
            materialsScope.Add(matPrim);

            var sectionOver = UsdPrim.Over("GeomSubset", $"Section_{i}");
            sectionOver.AddMetadata("prepend apiSchemas", UsdValue.Array(UsdValue.Token("MaterialBindingAPI")));
            sectionOver.Add(new UsdRelationship("material:binding", matPrim));
            meshOver.Add(sectionOver);
        }
    }

    private void BuildChildrenComponent(SceneComponentDto component, UsdPrim parentPrim, MeshLookup lookup, IReadOnlyDictionary<string, string>? worldPaths)
    {
        foreach (var child in component.Children)
        {
            // Cross-actor boundary → emit a full nested actor Scope
            if (child.Owner != component.Owner && child.Owner.RootComponent == child)
            {
                parentPrim.Add(BuildActorPrim(child.Owner, lookup, worldPaths));
                continue;
            }

            var childPrim = BuildComponentPrim(child, lookup);
            BuildChildrenComponent(child, childPrim, lookup, worldPaths);
            parentPrim.Add(childPrim);
        }
    }

    private UsdPrim BuildPointInstancer(InstancedStaticMeshComponentDto ism, MeshLookup lookup)
    {
        var instancer  = UsdPrim.Def("PointInstancer", "Instances");
        var prototypes = UsdPrim.Def("Scope", "Prototypes");

        // Build the prototype prim
        UsdPrim prototypePrim;
        if (lookup.TryGet(ism.MeshPtr, out var meshPath))
        {
            prototypePrim = new UsdPrim("Xform", ism.MeshPtr.Name);
            ApplyMaterialOverrides(prototypePrim, ism, ism.MeshPtr.Name);
            prototypePrim.SetReference(new UsdReferenceList([new UsdReference(meshPath)]));
        }
        else
        {
            prototypePrim = CreateDummyCube(ism.MeshPtr.Name);
        }

        prototypes.Add(prototypePrim);
        instancer.Add(prototypes); // must add before GetPath() is called on prototypePrim

        instancer.Add(new UsdRelationship("prototypes", prototypePrim));

        // Per-instance arrays – coordinate mirror matches the rest of the pipeline
        var count        = ism.Transforms.Length;
        var protoIndices = new UsdValue[count];
        var positions    = new UsdValue[count];
        var orientations = new UsdValue[count];
        var scales       = new UsdValue[count];

        for (var i = 0; i < count; i++)
        {
            var t = ism.Transforms[i].Translation;
            var r = ism.Transforms[i].Rotation;
            var s = ism.Transforms[i].Scale3D;

            protoIndices[i] = UsdValue.Int(0);
            positions[i]    = UsdValue.Tuple(t.X, -t.Y, t.Z);         // MIRROR_MESH
            orientations[i] = UsdValue.Tuple(r.W, -r.X, r.Y, -r.Z);  // quath (w,x,y,z) MIRROR_MESH
            scales[i]       = UsdValue.Tuple(s.X, s.Y, s.Z);
        }

        instancer.Add(new UsdAttribute("int[]",     "protoIndices", UsdValue.Array(protoIndices)));
        instancer.Add(new UsdAttribute("point3f[]", "positions",    UsdValue.Array(positions)));
        instancer.Add(new UsdAttribute("quath[]",   "orientations", UsdValue.Array(orientations)));
        instancer.Add(new UsdAttribute("float3[]",  "scales",       UsdValue.Array(scales)));

        return instancer;
    }

    private UsdPrim CreateDummyCube(string name = "Cube")
    {
        var mesh = UsdPrim.Def("Cube", name);
        return mesh;
    }

    private readonly record struct MeshLookup(IReadOnlyDictionary<FPackageIndex, string>? Paths)
    {
        public bool TryGet(FPackageIndex mesh, out string path)
        {
            path = string.Empty;
            return !mesh.IsNull && Paths is not null && Paths.TryGetValue(mesh, out path!);
        }
    }
}
