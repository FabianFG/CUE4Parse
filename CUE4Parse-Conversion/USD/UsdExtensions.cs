using System.Linq;
using System.Numerics;
using CUE4Parse_Conversion.V2.Dto;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.USD;

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
}
