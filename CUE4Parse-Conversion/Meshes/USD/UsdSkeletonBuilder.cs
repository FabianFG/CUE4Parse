using System.Collections.Generic;
using System.Linq;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Meshes.USD;

public static class UsdSkeletonBuilder
{
    public static UsdPrim Build(FReferenceSkeleton refSkeleton, string primName = "Skeleton")
    {
        var boneInfos = refSkeleton.FinalRefBoneInfo;
        var bonePoses = refSkeleton.FinalRefBonePose;
        var parents = boneInfos.Select(b => b.ParentIndex).ToArray();
        var jointPaths = BuildJointPaths(boneInfos.Select(b => (b.Name.Text, b.ParentIndex)).ToArray());
        var localMats = bonePoses.Select(t => ToMatrix(t.Translation, t.Rotation, t.Scale3D)).ToArray();
        return BuildPrim(primName, jointPaths, parents, localMats);
    }

    public static UsdPrim Build(IReadOnlyList<CSkelMeshBone> bones, string primName = "Skeleton")
    {
        var parents = bones.Select(b => b.ParentIndex).ToArray();
        var jointPaths = BuildJointPaths(bones.Select(b => (b.Name.Text, b.ParentIndex)).ToArray());
        var localMats = bones.Select(b => ToMatrix(b.Position, b.Orientation, b.Scale)).ToArray();
        return BuildPrim(primName, jointPaths, parents, localMats);
    }

    private static UsdPrim BuildPrim(string primName, string[] jointPaths, int[] parents, float[][,] localMats)
    {
        var prim = UsdPrim.Def("Skeleton", primName);
        prim.Add(UsdAttribute.Uniform("token[]", "joints", UsdValue.Array(jointPaths.Select(UsdValue.Token))));
        prim.Add(UsdAttribute.Uniform("matrix4d[]", "restTransforms", UsdValue.Array(localMats.Select(ToUsdValue))));

        // bindTransforms must be world-space.
        var worldMats = AccumulateWorldMatrices(localMats, parents);
        prim.Add(UsdAttribute.Uniform("matrix4d[]", "bindTransforms", UsdValue.Array(worldMats.Select(ToUsdValue))));
        return prim;
    }

    private static float[][,] AccumulateWorldMatrices(float[][,] localMats, int[] parents)
    {
        var world = new float[localMats.Length][,];
        for (var i = 0; i < localMats.Length; i++)
        {
            world[i] = parents[i] < 0
                ? localMats[i]
                : Multiply(localMats[i], world[parents[i]]);
        }
        return world;
    }

    /// <summary>Row-major 4×4 matrix multiply: C = A * B  (row-vector convention).</summary>
    private static float[,] Multiply(float[,] a, float[,] b)
    {
        var c = new float[4, 4];
        for (var r = 0; r < 4; r++)
            for (var col = 0; col < 4; col++)
                for (var k = 0; k < 4; k++)
                    c[r, col] += a[r, k] * b[k, col];
        return c;
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

    /// <summary>
    /// Converts a UE local-space bone transform to a row-major 4×4 double matrix,
    /// mirroring across the XZ plane (negate Y translation, reflect quaternion).
    /// </summary>
    private static float[,] ToMatrix(FVector translation, FQuat rotation, FVector scale)
    {
        // Mirror: negate Y translation; reflect quaternion across XZ plane → negate Qx and Qz.
        var tx =  translation.X;
        var ty = -translation.Y; // MIRROR_MESH
        var tz =  translation.Z;

        var qx = -rotation.X; // MIRROR_MESH
        var qy =  rotation.Y;
        var qz = -rotation.Z; // MIRROR_MESH
        var qw =  rotation.W;

        var sx = scale.X;
        var sy = scale.Y;
        var sz = scale.Z;

        // Row-major rotation matrix from normalized quaternion, with scale applied per row.
        return new[,]
        {
            { (1 - 2 * (qy * qy + qz * qz)) * sx, 2 * (qx * qy + qz * qw) * sx, 2 * (qx * qz - qy * qw) * sx, 0 },
            { 2 * (qx * qy - qz * qw) * sy, (1 - 2 * (qx * qx + qz * qz)) * sy, 2 * (qy * qz + qx * qw) * sy, 0 },
            { 2 * (qx * qz + qy * qw) * sz, 2 * (qy * qz - qx * qw) * sz, (1 - 2 * (qx * qx + qy * qy)) * sz, 0 },
            { tx, ty, tz, 1 }
        };
    }

    private static UsdValue ToUsdValue(float[,] m) => UsdValue.Tuple(
        UsdValue.Tuple(m[0, 0], m[0, 1], m[0, 2], m[0, 3]),
        UsdValue.Tuple(m[1, 0], m[1, 1], m[1, 2], m[1, 3]),
        UsdValue.Tuple(m[2, 0], m[2, 1], m[2, 2], m[2, 3]),
        UsdValue.Tuple(m[3, 0], m[3, 1], m[3, 2], m[3, 3])
    );
}
