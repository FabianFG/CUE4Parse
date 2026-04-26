using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CUE4Parse_Conversion.V2.Dto;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Meshes.USD;

public static class UsdSkeletonBuilder
{
    public static UsdPrim Build(FReferenceSkeleton refSkeleton, string primName = "Skeleton")
    {
        var boneInfos = refSkeleton.FinalRefBoneInfo;
        var parents = boneInfos.Select(b => b.ParentIndex).ToArray();
        var jointPaths = BuildJointPaths(boneInfos.Select(b => (b.Name.Text, b.ParentIndex)).ToArray());
        var localMats = refSkeleton.FinalRefBonePose.ToArray();
        return BuildPrim(primName, jointPaths, parents, localMats);
    }

    public static UsdPrim Build(IReadOnlyList<MeshBone> bones, string primName = "Skeleton")
    {
        var parents = bones.Select(b => b.ParentIndex).ToArray();
        var jointPaths = BuildJointPaths(bones.Select(b => (b.Name, b.ParentIndex)).ToArray());
        var localMats = bones.Select(b => b.Transform).ToArray();
        return BuildPrim(primName, jointPaths, parents, localMats);
    }

    private static UsdPrim BuildPrim(string primName, string[] jointPaths, int[] parents, FTransform[] localMats)
    {
        var matrices = new Matrix4x4[localMats.Length];
        for (var i = 0; i < localMats.Length; i++)
        {
            var t = localMats[i].Translation;
            var r = localMats[i].Rotation;
            var s = localMats[i].Scale3D;

            // Mirror Y to match the mesh coordinate space (mesh uses (X, -Y, Z))
            var mirroredTranslation = new Vector3(t.X, -t.Y, t.Z);
            // For a Y-mirror, the quaternion axis (qx, qy, qz) transforms to (-qx, qy, -qz)
            var mirroredRotation = new Quaternion(-r.X, r.Y, -r.Z, r.W);

            matrices[i] = Matrix4x4.CreateScale(s) *
                          Matrix4x4.CreateFromQuaternion(mirroredRotation) *
                          Matrix4x4.CreateTranslation(mirroredTranslation);
        }
        return BuildPrim(primName, jointPaths, parents, matrices);
    }

    private static UsdPrim BuildPrim(string primName, string[] jointPaths, int[] parents, Matrix4x4[] localMats)
    {
        var prim = UsdPrim.Def("Skeleton", primName);
        prim.Add(UsdAttribute.Uniform("token[]", "joints", UsdValue.Array(jointPaths.Select(UsdValue.Token))));
        prim.Add(UsdAttribute.Uniform("matrix4d[]", "restTransforms", UsdValue.Array(localMats.Select(ToUsdValue))));

        // bindTransforms must be world-space.
        var worldMats = AccumulateWorldMatrices(localMats, parents);
        prim.Add(UsdAttribute.Uniform("matrix4d[]", "bindTransforms", UsdValue.Array(worldMats.Select(ToUsdValue))));
        return prim;
    }

    private static Matrix4x4[] AccumulateWorldMatrices(Matrix4x4[] localMats, int[] parents)
    {
        var world = new Matrix4x4[localMats.Length];
        for (var i = 0; i < localMats.Length; i++)
        {
            world[i] = parents[i] < 0 ? localMats[i] : Matrix4x4.Multiply(localMats[i], world[parents[i]]);
        }
        return world;
    }

    private static string[] BuildJointPaths((string Name, int ParentIndex)[] bones)
    {
        var paths = new string[bones.Length];
        for (var i = 0; i < bones.Length; i++)
        {
            var (name, parentIndex) = bones[i];
            var sanitized = UsdMeshLodBuilder.SanitizePrimName(name) ?? $"Bone_{i}";
            paths[i] = parentIndex >= 0
                ? paths[parentIndex] + "/" + sanitized
                : sanitized;
        }
        return paths;
    }

    private static UsdValue ToUsdValue(Matrix4x4 m) => UsdValue.Tuple(
        UsdValue.Tuple(m[0, 0], m[0, 1], m[0, 2], m[0, 3]),
        UsdValue.Tuple(m[1, 0], m[1, 1], m[1, 2], m[1, 3]),
        UsdValue.Tuple(m[2, 0], m[2, 1], m[2, 2], m[2, 3]),
        UsdValue.Tuple(m[3, 0], m[3, 1], m[3, 2], m[3, 3])
    );
}
