using System.Collections.Generic;
using System.Linq;
using CUE4Parse_Conversion.USD;
using CUE4Parse_Conversion.V2.Dto.World;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.V2.Formats.World;

public class UsdWorldFormat : IWorldExportFormat
{
    public string DisplayName => "USD World (.usda)";

    public ExportFile Build(WorldDto dto, IReadOnlyDictionary<FPackageIndex, string>? meshes = null, IReadOnlyList<string>? subLayers = null)
    {
        var lookup = new MeshLookup(meshes);

        var worldPrim = UsdPrim.Def("Xform", dto.Name);
        foreach (var actor in dto.Actors)
        {
            worldPrim.Add(BuildActorPrim(actor, lookup));
        }

        var stage = new UsdStage(worldPrim);
        if (subLayers is { Count: > 0 })
        {
            stage.AddMetadata("subLayers", UsdValue.Array(subLayers.Select(UsdValue.AssetPath)));
        }

        return new ExportFile("usda", stage.SerializeToBinary());
    }

    private UsdPrim BuildActorPrim(ActorDto actor, MeshLookup lookup)
    {
        var actorPrim = UsdPrim.Def("Scope", actor.Name);
        actorPrim.AddPrimvar("token", "visibility", UsdValue.Token(actor.IsVisible ? "inherited" : "invisible"));

        if (actor.RootComponent is { } root)
        {
            var rootPrim = BuildComponentPrim(root, lookup);
            BuildChildrenComponent(root, rootPrim, lookup);
            actorPrim.Add(rootPrim);
        }

        foreach (var child in actor.ChildActors)
        {
            actorPrim.Add(BuildActorPrim(child, lookup));
        }

        return actorPrim;
    }

    private UsdPrim BuildComponentPrim(SceneComponentDto component, MeshLookup lookup)
    {
        string primName;
        UsdReference? reference = null;
        bool visible = true;

        switch (component)
        {
            case InstancedStaticMeshComponentDto:
                primName = component.Name;
                break;
            case StaticMeshComponentDto sm when lookup.TryGet(sm.StaticMesh, out var smPath):
                primName = sm.StaticMesh.Name;
                visible = sm.IsVisible;
                reference = new UsdReference(smPath);
                break;
            case SkeletalMeshComponentDto sk when lookup.TryGet(sk.SkinnedMesh, out var skPath):
                primName = sk.SkinnedMesh.Name;
                visible = sk.IsVisible;
                reference = new UsdReference(skPath);
                break;
            default:
                primName = component.Name;
                break;
        }

        var prim = UsdPrim.Def("Xform", component.Name);
        prim.AddPrimvar("token", "visibility", UsdValue.Token(visible ? "inherited" : "invisible"));
        prim.Add(component.Transform.ToTransformAttributes());

        if (reference is { } usdReference)
        {
            prim.SetReference(new UsdReferenceList([usdReference]));
        }

        if (component is InstancedStaticMeshComponentDto { Transforms.Length: > 0 } ism)
        {
            prim.Add(BuildPointInstancer(ism, lookup));
        }
        else if (reference == null && component is MeshComponentDto)
        {
            prim.Add(CreateDummyCube(primName));
        }

        return prim;
    }

    private void BuildChildrenComponent(SceneComponentDto component, UsdPrim parentPrim, MeshLookup lookup)
    {
        foreach (var child in component.Children)
        {
            // Cross-actor boundary → emit a full nested actor Scope
            if (child.Owner != component.Owner && child.Owner.RootComponent == child)
            {
                parentPrim.Add(BuildActorPrim(child.Owner, lookup));
                continue;
            }

            var childPrim = BuildComponentPrim(child, lookup);
            BuildChildrenComponent(child, childPrim, lookup);
            parentPrim.Add(childPrim);
        }
    }

    private UsdPrim BuildPointInstancer(InstancedStaticMeshComponentDto ism, MeshLookup lookup)
    {
        var instancer  = UsdPrim.Def("PointInstancer", "Instances");
        var prototypes = UsdPrim.Def("Scope", "Prototypes");

        // Build the prototype prim
        UsdPrim prototypePrim;
        if (lookup.TryGet(ism.StaticMesh, out var meshPath))
        {
            // Reference the shared mesh file – same asset, zero duplication
            prototypePrim = new UsdPrim("Xform", ism.StaticMesh.Name);
            prototypePrim.SetReference(new UsdReferenceList([new UsdReference(meshPath)]));
        }
        else
        {
            prototypePrim = CreateDummyCube(ism.StaticMesh.Name.Length > 0 ? ism.StaticMesh.Name : "Prototype");
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
        var mesh = UsdPrim.Def("Mesh", name);
        mesh.Add(UsdAttribute.Uniform("token", "subdivisionScheme", "none"));
        mesh.Add(UsdAttribute.Uniform("bool",  "doubleSided",       false));

        mesh.Add(new UsdAttribute("point3f[]", "points", UsdValue.Array(
            UsdValue.Tuple(-50f, -50f, -50f), UsdValue.Tuple( 50f, -50f, -50f),
            UsdValue.Tuple( 50f,  50f, -50f), UsdValue.Tuple(-50f,  50f, -50f),
            UsdValue.Tuple(-50f, -50f,  50f), UsdValue.Tuple( 50f, -50f,  50f),
            UsdValue.Tuple( 50f,  50f,  50f), UsdValue.Tuple(-50f,  50f,  50f))));

        mesh.Add(new UsdAttribute("int[]", "faceVertexCounts",
            UsdValue.Array(Enumerable.Repeat(3, 12))));

        mesh.Add(new UsdAttribute("int[]", "faceVertexIndices", UsdValue.Array(
            UsdValue.Int(0), UsdValue.Int(2), UsdValue.Int(1),
            UsdValue.Int(0), UsdValue.Int(3), UsdValue.Int(2),
            UsdValue.Int(4), UsdValue.Int(5), UsdValue.Int(6),
            UsdValue.Int(4), UsdValue.Int(6), UsdValue.Int(7),
            UsdValue.Int(0), UsdValue.Int(1), UsdValue.Int(5),
            UsdValue.Int(0), UsdValue.Int(5), UsdValue.Int(4),
            UsdValue.Int(2), UsdValue.Int(3), UsdValue.Int(7),
            UsdValue.Int(2), UsdValue.Int(7), UsdValue.Int(6),
            UsdValue.Int(0), UsdValue.Int(4), UsdValue.Int(7),
            UsdValue.Int(0), UsdValue.Int(7), UsdValue.Int(3),
            UsdValue.Int(1), UsdValue.Int(2), UsdValue.Int(6),
            UsdValue.Int(1), UsdValue.Int(6), UsdValue.Int(5))));

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
