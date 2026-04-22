using System;
using System.Collections.Generic;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse_Conversion.Meshes.USD;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.V2.Formats.Meshes;

public class UsdMeshFormat : IMeshExportFormat
{
    public string DisplayName => "USD Mesh (.usda)";

    public IReadOnlyList<ExportFile> BuildSkeletalMesh(string objectName, ExporterOptions options, USkeletalMesh originalMesh, CSkeletalMesh convertedMesh)
    {
        var results = new List<ExportFile>();
        var lodIdx = 0;

        var sockets = new List<FPackageIndex>();
        if (options.SocketFormat != ESocketFormat.None)
        {
            sockets.AddRange(originalMesh.Sockets);
            if (originalMesh.Skeleton.TryLoad<USkeleton>(out var originalSkeleton))
                sockets.AddRange(originalSkeleton.Sockets);
        }

        var refSkeleton = convertedMesh.RefSkeleton.Count > 0 ? convertedMesh.RefSkeleton : null;

        foreach (var lod in convertedMesh.LODs)
        {
            if (lod.SkipLod) continue;

            var meshPrim = UsdMeshLodBuilder.BuildFromLod(objectName, lod, convertedMesh.BoundingBox);
            var stage = AssembleSkeletalMeshStage(objectName, options, meshPrim, refSkeleton, sockets);
            var suffix = lodIdx == 0 ? "" : $"_LOD{lodIdx}";
            results.Add(stage.ToUsdaExportFile(suffix));
            lodIdx++;

            if (options.LodFormat == ELodFormat.FirstLod) break;
        }

        return results;
    }

    public IReadOnlyList<ExportFile> BuildStaticMesh(string objectName, ExporterOptions options, CStaticMesh convertedMesh)
    {
        var results = new List<ExportFile>();
        var lodIdx = 0;

        foreach (var lod in convertedMesh.LODs)
        {
            if (lod.SkipLod) continue;

            var meshPrim = UsdMeshLodBuilder.BuildFromLod(objectName, lod, convertedMesh.BoundingBox);
            var stage = AssembleStaticMeshStage(objectName, options, meshPrim, convertedMesh.Sockets);
            var suffix = lodIdx == 0 ? "" : $"_LOD{lodIdx}";
            results.Add(stage.ToUsdaExportFile(suffix));
            lodIdx++;

            if (options.LodFormat == ELodFormat.FirstLod) break;
        }

        return results;
    }

    public IReadOnlyList<ExportFile> BuildSkeleton(string objectName, ExporterOptions options, USkeleton skeleton)
    {
        var skelPrim = UsdSkeletonBuilder.Build(skeleton.ReferenceSkeleton);
        var root = UsdPrim.Def("SkelRoot", objectName);
        root.Add(skelPrim);

        var stage = new UsdStage(objectName);
        stage.Add(root);

        return [stage.ToUsdaExportFile()];
    }

    private static UsdStage AssembleSkeletalMeshStage(
        string objectName,
        ExporterOptions options,
        UsdPrim meshPrim,
        List<CSkelMeshBone>? refSkeleton,
        IEnumerable<FPackageIndex>? sockets)
    {
        var stage = new UsdStage(objectName);
        // SkelRoot is required by UsdSkel for any prim that participates in skinning.
        var root = UsdPrim.Def("SkelRoot", objectName);

        if (refSkeleton is not null)
        {
            var skelPrim = UsdSkeletonBuilder.Build(refSkeleton);
            root.Add(skelPrim);

            // Bind the mesh to the skeleton.
            meshPrim.Add(new UsdRelationship("skel:skeleton", $"/{objectName}/{skelPrim.Name}"));
        }

        root.Add(meshPrim);

        var socketsScope = BuildSocketsScope(options, sockets);
        if (socketsScope is not null)
            root.Add(socketsScope);

        stage.Add(root);
        return stage;
    }

    private static UsdStage AssembleStaticMeshStage(
        string objectName,
        ExporterOptions options,
        UsdPrim meshPrim,
        IEnumerable<FPackageIndex>? sockets)
    {
        var stage = new UsdStage(objectName);
        var root = UsdPrim.Def("Xform", objectName);

        root.Add(meshPrim);

        var socketsScope = BuildSocketsScope(options, sockets);
        if (socketsScope is not null)
            root.Add(socketsScope);

        stage.Add(root);
        return stage;
    }

    private readonly record struct SocketData(
        string Name,
        FVector Location,
        FRotator Rotation,
        FVector Scale,
        string? BoneName);

    private static SocketData? TryExtractSocket(FPackageIndex socketIndex)
    {
        if (socketIndex.Load<USkeletalMeshSocket>() is { } sk)
            return new SocketData(sk.SocketName.Text, sk.RelativeLocation, sk.RelativeRotation, sk.RelativeScale,
                BoneName: sk.BoneName.Text);

        if (socketIndex.Load<UStaticMeshSocket>() is { } st)
            return new SocketData(st.SocketName.Text, st.RelativeLocation, st.RelativeRotation, st.RelativeScale,
                BoneName: null);

        return null;
    }

    private static UsdPrim? BuildSocketsScope(ExporterOptions options, IEnumerable<FPackageIndex>? sockets)
    {
        if (options.SocketFormat == ESocketFormat.None || sockets is null) return null;

        var scope = UsdPrim.Def("Scope", "Sockets");
        var count = 0;

        foreach (var socketIndex in sockets)
        {
            if (TryExtractSocket(socketIndex) is not { } socket) continue;

            var socketName = UsdMeshLodBuilder.SanitizePrimName(socket.Name) ?? $"Socket_{count}";
            var socketPrim = UsdPrim.Def("Xform", socketName);

            socketPrim.Add(UsdAttribute.Uniform("token[]", "xformOpOrder", UsdValue.Array("xformOp:translate", "xformOp:rotateXYZ", "xformOp:scale")));
            socketPrim.Add(new UsdAttribute("double3", "xformOp:translate",
                UsdValue.Tuple(socket.Location.X, socket.Location.Y, socket.Location.Z)));
            socketPrim.Add(new UsdAttribute("double3", "xformOp:rotateXYZ",
                UsdValue.Tuple(socket.Rotation.Roll, socket.Rotation.Pitch, socket.Rotation.Yaw)));
            socketPrim.Add(new UsdAttribute("double3", "xformOp:scale",
                UsdValue.Tuple(socket.Scale.X, socket.Scale.Y, socket.Scale.Z)));

            if (!string.IsNullOrWhiteSpace(socket.BoneName))
                socketPrim.Add(UsdAttribute.CustomUniform("string", "unrealBoneName", socket.BoneName));

            scope.Add(socketPrim);
            count++;
        }

        return count > 0 ? scope : null;
    }
}
