using System;
using System.Collections.Generic;
using System.IO;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Writers;
using CUE4Parse_Conversion.Materials;
using CUE4Parse_Conversion.Meshes.glTF;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse_Conversion.Meshes.UEFormat;
using CUE4Parse.UE4.Objects.PhysicsEngine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using Serilog;

namespace CUE4Parse_Conversion.Meshes
{
    public class MeshExporter : ExporterBase
    {
        public readonly List<Mesh> MeshLods;

        public MeshExporter(USkeleton originalSkeleton, ExporterOptions options) : base(originalSkeleton, options)
        {
            MeshLods = new List<Mesh>();

            if (!originalSkeleton.TryConvert(out var bones, out _) || bones.Count == 0)
            {
                Log.Logger.Warning($"Skeleton '{ExportName}' has no bone");
                return;
            }

            using var Ar = new FArchiveWriter();
            string ext;
            switch (Options.MeshFormat)
            {
                case EMeshFormat.ActorX:
                    ext = "pskx";
                    new ActorXMesh(bones, originalSkeleton.Sockets, Options).Save(Ar);
                    break;
                case EMeshFormat.UEFormat:
                    ext = "uemodel";
                    new UEModel(originalSkeleton.Name, bones, originalSkeleton.Sockets, originalSkeleton.VirtualBones, Options).Save(Ar);
                    break;
                case EMeshFormat.Gltf2:
                    throw new NotImplementedException();
                case EMeshFormat.OBJ:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException(nameof(Options.MeshFormat), Options.MeshFormat, null);
            }

            MeshLods.Add(new Mesh($"{PackagePath}.{ext}", Ar.GetBuffer(), new List<MaterialExporter2>()));
        }

        public MeshExporter(UStaticMesh originalMesh, ExporterOptions options) : base(originalMesh, options)
        {
            MeshLods = new List<Mesh>();

            if (!originalMesh.TryConvert(out var convertedMesh) || convertedMesh.LODs.Count == 0)
            {
                Log.Logger.Warning($"Mesh '{ExportName}' has no LODs");
                return;
            }

            if (Options.MeshFormat == EMeshFormat.UEFormat)
            {
                using var ueModelArchive = new FArchiveWriter();
                new UEModel(originalMesh.Name, convertedMesh, originalMesh.BodySetup, Options).Save(ueModelArchive);
                MeshLods.Add(new Mesh($"{PackagePath}.uemodel", ueModelArchive.GetBuffer(), convertedMesh.LODs[0].GetMaterials(options)));
                return;
            }

            var i = -1;
            foreach (var lod in convertedMesh.LODs)
            {
                i++;
                if (lod.SkipLod)
                {
                    Log.Logger.Warning($"LOD {i} in mesh '{ExportName}' should be skipped");
                    continue;
                }

                using var Ar = new FArchiveWriter();
                var materialExports = options.ExportMaterials ? new List<MaterialExporter2>() : null;
                string ext;
                switch (Options.MeshFormat)
                {
                    case EMeshFormat.ActorX:
                        ext = "pskx";
                        new ActorXMesh(lod, materialExports, originalMesh.Sockets, Options).Save(Ar);
                        break;
                    case EMeshFormat.Gltf2:
                        ext = "glb";
                        new Gltf(ExportName, lod, materialExports, Options).Save(Options.MeshFormat, Ar);
                        break;
                    case EMeshFormat.OBJ:
                        ext = "obj";
                        new Gltf(ExportName, lod, materialExports, Options).Save(Options.MeshFormat, Ar);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Options.MeshFormat), Options.MeshFormat, null);
                }

                if (Options.LodFormat == ELodFormat.FirstLod)
                {
                    MeshLods.Add(new Mesh($"{PackagePath}.{ext}", Ar.GetBuffer(), materialExports ?? new List<MaterialExporter2>()));
                    break;
                }
                else
                {
                    MeshLods.Add(new Mesh($"{PackagePath}_LOD{i}.{ext}", Ar.GetBuffer(), materialExports ?? new List<MaterialExporter2>()));
                }
            }
        }

        public MeshExporter(USkeletalMesh originalMesh, ExporterOptions options) : base(originalMesh, options)
        {
            MeshLods = new List<Mesh>();

            if (!originalMesh.TryConvert(out var convertedMesh) || convertedMesh.LODs.Count == 0)
            {
                Log.Logger.Warning($"Mesh '{ExportName}' has no LODs");
                return;
            }

            var totalSockets = new List<FPackageIndex>();
            if (Options.SocketFormat != ESocketFormat.None)
            {
                totalSockets.AddRange(originalMesh.Sockets);
                if (originalMesh.Skeleton.TryLoad<USkeleton>(out var originalSkeleton))
                {
                    totalSockets.AddRange(originalSkeleton.Sockets);
                }
            }

            if (Options.ExportMorphTargets) originalMesh.PopulateMorphTargetVerticesData();

            if (Options.MeshFormat == EMeshFormat.UEFormat)
            {
                using var ueModelArchive = new FArchiveWriter();
                new UEModel(originalMesh.Name, convertedMesh, originalMesh.MorphTargets, totalSockets.ToArray(), originalMesh.PhysicsAsset, Options).Save(ueModelArchive);
                MeshLods.Add(new Mesh($"{PackagePath}.uemodel", ueModelArchive.GetBuffer(), convertedMesh.LODs[0].GetMaterials(options)));
                return;
            }

            var i = 0;
            for (var lodIndex = 0; lodIndex < convertedMesh.LODs.Count; lodIndex++)
            {
                var lod = convertedMesh.LODs[lodIndex];
                if (lod.SkipLod)
                {
                    Log.Logger.Warning($"LOD {i} in mesh '{ExportName}' should be skipped");
                    continue;
                }

                using var Ar = new FArchiveWriter();
                var materialExports = options.ExportMaterials ? new List<MaterialExporter2>() : null;
                var ext = "";
                switch (Options.MeshFormat)
                {
                    case EMeshFormat.ActorX:
                        ext = convertedMesh.LODs[i].NumVerts > 65536 ? "pskx" : "psk";
                        new ActorXMesh(lod, convertedMesh.RefSkeleton, materialExports,
                            Options.ExportMorphTargets ? originalMesh.MorphTargets : null,
                            totalSockets.ToArray(), lodIndex, Options).Save(Ar);
                        break;
                    case EMeshFormat.Gltf2:
                        ext = "glb";
                        new Gltf(ExportName, lod, convertedMesh.RefSkeleton, materialExports, Options,
                            Options.ExportMorphTargets ? originalMesh.MorphTargets : null,
                            lodIndex).Save(Options.MeshFormat, Ar);
                        break;
                    case EMeshFormat.OBJ:
                        ext = "obj";
                        new Gltf(ExportName, lod, convertedMesh.RefSkeleton, materialExports, Options).Save(Options.MeshFormat, Ar);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Options.MeshFormat), Options.MeshFormat, null);
                }

                if (Options.LodFormat == ELodFormat.FirstLod)
                {
                    MeshLods.Add(new Mesh($"{PackagePath}.{ext}", Ar.GetBuffer(), materialExports ?? new List<MaterialExporter2>()));
                    break;
                }
                else
                {
                    MeshLods.Add(new Mesh($"{PackagePath}_LOD{i}.{ext}", Ar.GetBuffer(), materialExports ?? new List<MaterialExporter2>()));
                }
                i++;
            }
        }

        /// <param name="baseDirectory"></param>
        /// <param name="label"></param>
        /// <param name="savedFilePath"></param>
        /// <returns>true if *ALL* lods were successfully exported</returns>
        public override bool TryWriteToDir(DirectoryInfo baseDirectory, out string label, out string savedFilePath)
        {
            var b = false;
            label = string.Empty;
            savedFilePath = PackagePath;
            if (MeshLods.Count == 0) return b;

            var outText = "LOD ";
            for (var i = 0; i < MeshLods.Count; i++)
            {
                b |= MeshLods[i].TryWriteToDir(baseDirectory, out label, out savedFilePath);
                outText += $"{i} ";
            }

            label = outText + $"as '{savedFilePath.SubstringAfterWithLast('.')}' for '{ExportName}'";
            return b;
        }

        public override bool TryWriteToZip(out byte[] zipFile)
        {
            throw new NotImplementedException();
        }

        public override void AppendToZip()
        {
            throw new NotImplementedException();
        }
    }
}
