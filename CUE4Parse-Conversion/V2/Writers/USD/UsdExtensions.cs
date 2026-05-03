using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CUE4Parse_Conversion.V2.Dto;
using CUE4Parse_Conversion.V2.Dto.World;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse_Conversion.V2.Writers.USD;

public static class UsdExtensions
{
    public static UsdAttribute[] ToMatrixAttributes(this FTransform transform) => transform.ToMatrix4x4().ToAttributes();

    public static UsdAttribute[] ToTransformAttributes(this FTransform transform)
    {
        var t = transform.Translation;
        var r = transform.Rotation;
        var s = transform.Scale3D;

        // MIRROR_MESH
        return
        [
            UsdAttribute.Uniform("token[]", "xformOpOrder", UsdValue.Array(
                UsdValue.Token("xformOp:translate"),
                UsdValue.Token("xformOp:orient"),
                UsdValue.Token("xformOp:scale")
            )),
            new("float3", "xformOp:translate", UsdValue.From(new Vector3(t.X, -t.Y, t.Z))),
            new("quatf", "xformOp:orient", UsdValue.From(new Quaternion(-r.X, r.Y, -r.Z, r.W))),
            new("float3", "xformOp:scale", UsdValue.From(new Vector3(s.X, s.Y, s.Z))),
        ];
    }

    public static UsdAttribute[] ToAttributes(this Matrix4x4 matrix) =>
    [
        UsdAttribute.Uniform("token[]", "xformOpOrder", UsdValue.Array(UsdValue.Token("xformOp:transform"))),
        new("matrix4d", "xformOp:transform", UsdValue.From(matrix)),
    ];

    public static Matrix4x4 ToMatrix4x4(this FTransform transform)
    {
        var t = transform.Translation;
        var r = transform.Rotation;
        var s = transform.Scale3D;

        // MIRROR_MESH
        return Matrix4x4.CreateScale(new Vector3(s.X, s.Y, s.Z)) *
               Matrix4x4.CreateFromQuaternion(new Quaternion(-r.X, r.Y, -r.Z, r.W)) *
               Matrix4x4.CreateTranslation(new Vector3(t.X, -t.Y, t.Z));
    }

    public static UsdPrim ToSkelRoot(this Skeleton dto)
    {
        var root = UsdPrim.Def("SkelRoot", dto.Name);
        var skeletonPrim = UsdPrim.Def("Skeleton", dto.SkeletonName ?? root.Name);

        var joints = new UsdValue[dto.Bones.Length];
        var rest = new UsdValue[joints.Length];
        var bind = new Matrix4x4[joints.Length]; // world-space accumulated
        for (var i = 0; i < joints.Length; i++)
        {
            var bone = dto.Bones[i];

            var path = bone.ParentIndex >= 0 ? $"{joints[bone.ParentIndex].RawValue}/{bone.Name}" : bone.Name;
            joints[i] = UsdValue.Token(path);

            var local = bone.Transform.ToMatrix4x4();
            rest[i] = UsdValue.From(local);

            bind[i] = bone.ParentIndex < 0 ? local : Matrix4x4.Multiply(local, bind[bone.ParentIndex]);
        }

        skeletonPrim.Add(UsdAttribute.Uniform("token[]", "joints", UsdValue.Array(joints)));
        skeletonPrim.Add(UsdAttribute.Uniform("matrix4d[]", "restTransforms", UsdValue.Array(rest)));
        skeletonPrim.Add(UsdAttribute.Uniform("matrix4d[]", "bindTransforms", UsdValue.Array(bind.Select(x => UsdValue.From(x)))));

        root.Add(skeletonPrim);
        return root;
    }

    public static UsdPrim ToMeshPrim(this BrushComponentDto brush)
    {
        var prim = UsdPrim.Def("Mesh", brush.Name);
        prim.Add(UsdAttribute.Uniform("token", "subdivisionScheme", UsdValue.Token("none")));
        prim.Add(UsdAttribute.Uniform("token", "purpose", UsdValue.Token("guide")));
        prim.Add(UsdAttribute.Uniform("token", "model:drawMode", UsdValue.Token("bounds")));
        prim.Add(UsdAttribute.Uniform("bool", "model:applyDrawMode", true));

        var model = brush.BrushPtr.Load<UModel>();
        if (model is null) return prim;

        var points = model.Points;
        var nodes  = model.Nodes;
        var verts  = model.Verts;
        if (points.Length == 0 || nodes.Length == 0 || verts.Length == 0) return prim;

        var positions         = new List<UsdValue>();
        var faceVertexCounts  = new List<int>();
        var faceVertexIndices = new List<int>();
        var pointIndexMap     = new Dictionary<int, int>();

        foreach (var node in nodes)
        {
            if (node.NumVertices < 3) continue;
            var pool = node.iVertPool;
            if (pool < 0 || pool + node.NumVertices > verts.Length) continue;

            // Validate all vertex references
            var valid = true;
            for (var i = 0; i < node.NumVertices && valid; i++)
                valid = verts[pool + i].pVertex is >= 0 and var pv && pv < points.Length;
            if (!valid) continue;

            // Resolve to output indices, deduplicating shared points
            var nodeIndices = new int[node.NumVertices];
            for (var i = 0; i < node.NumVertices; i++)
            {
                var pv = verts[pool + i].pVertex;
                if (!pointIndexMap.TryGetValue(pv, out var outIdx))
                {
                    outIdx = positions.Count;
                    pointIndexMap[pv] = outIdx;
                    var p = points[pv];
                    positions.Add(UsdValue.Tuple(p.X, -p.Y, p.Z)); // MIRROR_MESH
                }
                nodeIndices[i] = outIdx;
            }

            // Fan triangulation from vertex 0
            for (var i = 1; i < node.NumVertices - 1; i++)
            {
                faceVertexCounts.Add(3);
                faceVertexIndices.Add(nodeIndices[0]);
                faceVertexIndices.Add(nodeIndices[i]);
                faceVertexIndices.Add(nodeIndices[i + 1]);
            }
        }

        if (positions.Count == 0) return prim;

        prim.Add(new UsdAttribute("point3f[]", "points",           UsdValue.Array(positions)));
        prim.Add(new UsdAttribute("int[]",     "faceVertexCounts", UsdValue.Array(faceVertexCounts)));
        prim.Add(new UsdAttribute("int[]",     "faceVertexIndices",UsdValue.Array(faceVertexIndices)));

        return prim;
    }
    public static UsdPrim ToShapePrim(this ShapeComponentDto shape)
    {
        const float scale = 100;

        UsdPrim prim;
        switch (shape)
        {
            case BoxComponentDto box:
            {
                var e = box.BoxExtent * scale;
                prim = UsdPrim.Def("Cube", shape.Name);
                prim.Add(new UsdAttribute("double", "size", UsdValue.Double(2)));
                prim.Add(UsdAttribute.Uniform("token[]", "xformOpOrder", UsdValue.Array(UsdValue.Token("xformOp:scale"))));
                prim.Add(new UsdAttribute("float3", "xformOp:scale", UsdValue.Tuple(e.X, e.Y, e.Z)));
                break;
            }
            case SphereComponentDto sphere:
            {
                prim = UsdPrim.Def("Sphere", shape.Name);
                prim.Add(new UsdAttribute("double", "radius", UsdValue.Double(sphere.SphereRadius * scale)));
                break;
            }
            case CapsuleComponentDto capsule:
            {
                prim = UsdPrim.Def("Capsule", shape.Name);
                prim.Add(new UsdAttribute("double", "height", UsdValue.Double(capsule.CapsuleHalfHeight * 2 * scale)));
                prim.Add(new UsdAttribute("double", "radius", UsdValue.Double(capsule.CapsuleRadius * scale)));
                prim.Add(UsdAttribute.Uniform("token", "axis", "Z"));
                break;
            }
            default: throw new NotSupportedException($"Unsupported shape type: {shape.GetType().Name}");
        }

        prim.Add(UsdAttribute.Uniform("token", "purpose", UsdValue.Token("guide")));
        prim.Add(UsdAttribute.Uniform("token", "model:drawMode", UsdValue.Token("bounds")));
        prim.Add(UsdAttribute.Uniform("bool", "model:applyDrawMode", true));
        return prim;
    }
}
