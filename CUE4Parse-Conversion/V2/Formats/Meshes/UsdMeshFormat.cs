using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Meshes.USD;
using CUE4Parse_Conversion.V2.Dto;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.V2.Formats.Meshes;

public class UsdMeshFormat : IMeshExportFormat
{
    public string DisplayName => "USD Mesh (.usda)";

    public IReadOnlyList<ExportFile> BuildSkeletalMesh(string objectName, ExporterOptions options, SkeletalMesh dto)
    {
        var results = new List<ExportFile>();
        var root = UsdPrim.Def("SkelRoot", dto.Name);
        root.Add(CreateSkeleton(dto));

        var sockets = options.SocketFormat != ESocketFormat.None ? CreateSockets(dto.Sockets) : null;
        if (sockets is not null) root.Add(sockets);

        for (var i = 0; i < dto.LODs.Count; i++)
        {
            var suffix = i == 0 ? null : $"_LOD{i}";
            var lodPrim = CreateLod(dto.LODs[i], suffix);
            lodPrim.Add(new UsdRelationship("skel:skeleton", root.Children[0]));
            root.Add(lodPrim);

            var stage = new UsdStage(root);
            results.Add(new ExportFile("usda", stage.SerializeToBinary(), suffix));

            if (options.LodFormat == ELodFormat.FirstLod) break;
            root.Children.RemoveAt(root.Children.Count - 1);
        }

        return results;
    }

    public IReadOnlyList<ExportFile> BuildStaticMesh(string objectName, ExporterOptions options, StaticMesh dto)
    {
        var results = new List<ExportFile>();
        var root = UsdPrim.Def("Xform", dto.Name);

        var sockets = options.SocketFormat != ESocketFormat.None ? CreateSockets(dto.Sockets) : null;
        if (sockets is not null) root.Add(sockets);

        for (var i = 0; i < dto.LODs.Count; i++)
        {
            var suffix = i == 0 ? null : $"_LOD{i}";
            root.Add(CreateLod(dto.LODs[i], suffix));

            var stage = new UsdStage(root);
            results.Add(new ExportFile("usda", stage.SerializeToBinary(), suffix));

            if (options.LodFormat == ELodFormat.FirstLod) break;
            root.Children.RemoveAt(root.Children.Count - 1);
        }

        return results;
    }

    public IReadOnlyList<ExportFile> BuildSkeleton(string objectName, ExporterOptions options, Skeleton dto)
    {
        var root = UsdPrim.Def("SkelRoot", dto.Name);
        root.Add(CreateSkeleton(dto));

        var sockets = options.SocketFormat != ESocketFormat.None ? CreateSockets(dto.Sockets) : null;
        if (sockets is not null) root.Add(sockets);

        var stage = new UsdStage(root);
        return [new ExportFile("usda", stage.SerializeToBinary())];
    }

    private UsdPrim CreateSkeleton(Skeleton dto)
    {
        var skeletonPrim = UsdPrim.Def("Skeleton", dto.SkeletonName ?? dto.Name);

        var joints = new UsdValue[dto.RefSkeleton.Length];
        var rest = new UsdValue[joints.Length];
        var bind = new Matrix4x4[joints.Length]; // world-space accumulated
        for (var i = 0; i < joints.Length; i++)
        {
            var bone = dto.RefSkeleton[i];

            var path = bone.ParentIndex >= 0 ? $"{joints[bone.ParentIndex].RawValue}/{bone.Name}" : bone.Name;
            joints[i] = UsdValue.Token(path);

            var local = bone.Transform.ToMatrix4x4();
            rest[i] = UsdValue.From(local);

            bind[i] = bone.ParentIndex < 0 ? local : Matrix4x4.Multiply(local, bind[bone.ParentIndex]);
        }

        skeletonPrim.Add(UsdAttribute.Uniform("token[]", "joints", UsdValue.Array(joints)));
        skeletonPrim.Add(UsdAttribute.Uniform("matrix4d[]", "restTransforms", UsdValue.Array(rest)));
        skeletonPrim.Add(UsdAttribute.Uniform("matrix4d[]", "bindTransforms", UsdValue.Array(bind.Select(x => UsdValue.From(x)))));

        return skeletonPrim;
    }

    private UsdPrim? CreateSockets(FPackageIndex[]? sockets)
    {
        if (sockets is not { Length: > 0 }) return null;

        var scope = UsdPrim.Def("Scope", "Sockets");
        foreach (var ptr in sockets)
        {
            switch (ptr.Load())
            {
                case USkeletalMeshSocket sk:
                {
                    var socketPrim = UsdPrim.Def("Xform", sk.SocketName.Text);
                    // TODO: compute matrix relative to bone
                    socketPrim.Add(new FTransform(sk.RelativeRotation, sk.RelativeLocation, sk.RelativeScale).ToAttributes());
                    socketPrim.Add(UsdAttribute.CustomUniform("string", "unrealBoneName", sk.BoneName.Text));
                    scope.Add(socketPrim);
                    break;
                }
                case UStaticMeshSocket st:
                {
                    var socketPrim = UsdPrim.Def("Xform", st.SocketName.Text);
                    socketPrim.Add(new FTransform(st.RelativeRotation, st.RelativeLocation, st.RelativeScale).ToAttributes());
                    scope.Add(socketPrim);
                    break;
                }
            }
        }
        return scope;
    }

    private UsdPrim CreateLod(MeshLod<MeshVertex> meshLod, string? suffix = null) => CreateLod<MeshVertex>(meshLod, suffix);
    private UsdPrim CreateLod(MeshLod<SkinnedMeshVertex> meshLod, string? suffix = null)
    {
        var lodPrim = CreateLod<SkinnedMeshVertex>(meshLod, suffix);
        lodPrim.AddMetadata("prepend apiSchemas", UsdValue.Array(UsdValue.Token("SkelBindingAPI")));

        var elementSize = meshLod.Vertices.Max(v => v.Influences.Length);
        var indices = new UsdValue[meshLod.Vertices.Length * elementSize];
        var weights = new UsdValue[indices.Length];
        for (var i = 0; i < meshLod.Vertices.Length; i++)
        {
            var influence = meshLod.Vertices[i].Influences;
            for (var j = 0; j < elementSize; j++)
            {
                var bone = 0;
                var weight = 0f;
                if (j < influence.Length)
                {
                    bone = influence[j].Bone;
                    weight = influence[j].Weight;
                }

                indices[i * elementSize + j] = UsdValue.Int(bone);
                weights[i * elementSize + j] = UsdValue.Float(weight);
            }
        }

        var metadata = new UsdMetadata("elementSize", elementSize);
        lodPrim.AddPrimvar("int[]", "primvars:skel:jointIndices", UsdValue.Array(indices), "vertex", metadata);
        lodPrim.AddPrimvar("float[]", "primvars:skel:jointWeights", UsdValue.Array(weights), "vertex", metadata);

        return lodPrim;
    }
    private UsdPrim CreateLod<TVertex>(MeshLod<TVertex> meshLod, string? suffix = null) where TVertex : struct, IMeshVertex
    {
        var lodPrim = UsdPrim.Def("Mesh", $"{meshLod.Owner.Name}{suffix}");
        lodPrim.Add(UsdAttribute.Uniform("token", "subdivisionScheme", "none"));
        lodPrim.Add(UsdAttribute.Uniform("bool", "doubleSided", meshLod.IsTwoSided));

        var bounds = meshLod.Owner.Bounds;
        lodPrim.Add(new UsdAttribute("float3[]", "extent", UsdValue.Array(
            UsdValue.Tuple(bounds.Min.X, bounds.Min.Y, bounds.Min.Z),
            UsdValue.Tuple(bounds.Max.X, bounds.Max.Y, bounds.Max.Z))));

        var points = new UsdValue[meshLod.Vertices.Length];
        var normals = new UsdValue[points.Length];
        var tangents = new UsdValue[points.Length];
        var uv = new UsdValue[points.Length];
        for (var i = 0; i < points.Length; i++)
        {
            var position = meshLod.Vertices[i].Position;
            points[i] = UsdValue.Tuple(position.X, -position.Y, position.Z); // MIRROR_MESH

            var normal = (FVector) meshLod.Vertices[i].Normal;
            normal /= MathF.Sqrt(normal | normal);
            normals[i] = UsdValue.Tuple(normal.X, -normal.Y, normal.Z); // MIRROR_MESH

            var tangent = (FVector) meshLod.Vertices[i].Tangent;
            tangents[i] = UsdValue.Tuple(tangent.X, -tangent.Y, tangent.Z); // MIRROR_MESH

            uv[i] = UsdValue.Tuple(meshLod.Vertices[i].Uv.U, meshLod.Vertices[i].Uv.V);
        }

        var indices = new UsdValue[meshLod.Indices.Length];
        for (var i = 0; i < indices.Length; i++)
        {
            indices[i] = UsdValue.Int((int) meshLod.Indices[i]);
        }

        lodPrim.Add(new UsdAttribute("point3f[]", "points", UsdValue.Array(points)));
        lodPrim.Add(new UsdAttribute("int[]", "faceVertexCounts", UsdValue.Array(Enumerable.Repeat(3, indices.Length / 3))));
        lodPrim.Add(new UsdAttribute("int[]", "faceVertexIndices", UsdValue.Array(indices)));

        lodPrim.AddPrimvar("normal3f[]", "normals", UsdValue.Array(normals), "vertex");
        lodPrim.AddPrimvar("float3[]", "primvars:tangents", UsdValue.Array(tangents), "vertex");
        lodPrim.AddPrimvar("texCoord2f[]", "primvars:st", UsdValue.Array(uv), "vertex");

        for (var i = 0; i < meshLod.ExtraUvs.Length; i++)
        {
            var extraUv = new UsdValue[meshLod.ExtraUvs[i].Length];
            for (var j = 0; j < extraUv.Length; j++)
            {
                extraUv[j] = UsdValue.Tuple(meshLod.ExtraUvs[i][j].U, meshLod.ExtraUvs[i][j].V);
            }
            lodPrim.AddPrimvar("texCoord2f[]", $"primvars:st{i + 1}", UsdValue.Array(extraUv), "vertex");
        }

        if (meshLod.VertexColors is { Length: > 0 } && meshLod.VertexColors[0].Colors is { Length: > 0 } vertexColors)
        {
            var colors = new UsdValue[vertexColors.Length];
            var opacities = new UsdValue[colors.Length];
            for (var i = 0; i < colors.Length; i++)
            {
                var color = vertexColors[i];
                colors[i] = UsdValue.Tuple(color.R / 255f, color.G / 255f, color.B / 255f);
                opacities[i] = UsdValue.Float(color.A / 255f);
            }

            lodPrim.AddPrimvar("color3f[]", "primvars:displayColor", UsdValue.Array(colors), "vertex");
            lodPrim.AddPrimvar("float[]", "primvars:displayOpacity", UsdValue.Array(opacities), "vertex");
        }

        for (var i = 0; i < meshLod.Sections.Length; i++)
        {
            var subset = UsdPrim.Def("GeomSubset", $"Section_{i}");
            subset.Add(UsdAttribute.Uniform("token", "elementType", "face"));
            subset.Add(UsdAttribute.Uniform("token", "familyName", "materialBind"));

            var section = meshLod.Sections[i];
            subset.Add(new UsdAttribute("int[]", "indices", UsdValue.Array(Enumerable.Range(section.FirstIndex / 3, section.NumFaces))));
            subset.Add(UsdAttribute.CustomUniform("int", "unrealMaterialIndex", section.MaterialIndex));
            subset.Add(UsdAttribute.CustomUniform("bool", "unrealCastShadow", section.CastShadow));

            if (meshLod.Owner.GetMaterial(section) is { } material)
            {
                subset.AddMetadata("prepend apiSchemas", UsdValue.Array(UsdValue.Token("MaterialBindingAPI")));

                var materialPrim = UsdPrim.Def("Material", material.SlotName);
                // TODO: define the prim
                lodPrim.Add(materialPrim);

                subset.Add(new UsdRelationship("material:binding", materialPrim));
            }

            lodPrim.Add(subset);
        }

        return lodPrim;
    }
}
