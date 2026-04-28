using System.Numerics;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Meshes.USD;

public static class UsdExtensions
{
    public static UsdAttribute[] ToAttributes(this FTransform transform) => transform.ToMatrix4x4().ToAttributes();

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
}
