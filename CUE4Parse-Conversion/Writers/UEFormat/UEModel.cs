using System;
using System.Collections.Generic;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Writers.UEFormat.Structs;
using CUE4Parse_Conversion.Writers.UEFormat.Structs.Collision;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.PhysicsEngine;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.Writers.UEFormat;

public sealed class UEModel : UEFormatExport
{
    protected override string Identifier => "UEMODEL";

    public UEModel(string name, string objectPath, StaticMeshDto mesh, ExportOptions options, Func<MeshLodDto<MeshVertex>, bool>? predicate = null)
        : base(name, objectPath, options)
    {
        WriteRoot(root =>
        {
            WriteLods(root, FilterLods(mesh.LODs, predicate), WriteCommonMesh);
            WriteCollision(root, mesh);
        });
    }

    public UEModel(string name, string objectPath, SkeletonDto skeleton, ExportOptions options)
        : base(name, objectPath, options)
    {
        WriteRoot(root => WriteSkeleton(root, skeleton, options.SocketFormat != ESocketFormat.None));
    }

    public UEModel(string name, string objectPath, SkeletalMeshDto mesh, ExportOptions options, Func<MeshLodDto<SkinnedMeshVertex>, bool>? predicate = null)
        : base(name, objectPath, options)
    {
        WriteRoot(root =>
        {
            var lods = FilterLods(mesh.LODs, predicate);
            WriteLods(root, lods, (attrs, lod) =>
            {
                WriteCommonMesh(attrs, lod);
                WriteSkinning(attrs, lod, options.ExportMorphTargets ? mesh.MorphTargets : []);
            });

            WriteSkeleton(root, mesh, options.SocketFormat != ESocketFormat.None);
        });
    }

    private static void WriteLods<TVertex>(
        FDataAttributeSet root,
        IReadOnlyList<MeshLodDto<TVertex>> lods,
        Action<FDataAttributeSet, MeshLodDto<TVertex>> writeLodAttrs) where TVertex : struct, IMeshVertex
    {
        if (lods.Count == 0) return;

        root.AddAttribute("LODS", attr => attr.WriteArray(lods, (writer, lod) =>
        {
            writer.WriteFString(lod.IsNanite ? "Nanite" : $"LOD{lod.SourceLodIndex}");
            writer.WriteAttributes(attrs => writeLodAttrs(attrs, lod));
        }));
    }

    private static void WriteCollision(FDataAttributeSet root, StaticMeshDto mesh)
    {
        if (mesh.BodySetup is null) return;
        if (!mesh.BodySetup.TryLoad<UBodySetup>(out var bodySetup)) return;
        if (bodySetup.AggGeom?.ConvexElems is not { Length: > 0 } convexElems) return;

        root.AddAttribute("COLLISION", attr => attr.WriteArray(convexElems, (writer, elem) =>
            new FConvexMeshCollision(elem).Serialize(writer)));
    }

    private static void WriteSkeleton(FDataAttributeSet root, SkeletonDto skeleton, bool exportSockets)
    {
        root.AddAttribute("SKELETON", attr => attr.WriteAttributes(attrs =>
        {
            attrs.AddAttribute("METADATA", attr => attr.WriteFString(skeleton.SkeletonPathName ?? "Skeleton"));

            attrs.AddAttribute("BONES", attr => attr.WriteArray(skeleton.Bones, (writer, bone) =>
            {
                writer.WriteFString(bone.Name);
                writer.Write(bone.ParentIndex);
                bone.Transform.Translation.Serialize(writer);
                bone.Transform.Rotation.Serialize(writer);
                bone.Transform.Scale3D.Serialize(writer);
            }));

            if (exportSockets && skeleton.Sockets is { Length: > 0 })
            {
                var sockets = new List<USkeletalMeshSocket>();
                foreach (var socketObject in skeleton.Sockets)
                {
                    if (socketObject.Load<USkeletalMeshSocket>() is { } socket)
                        sockets.Add(socket);
                }

                if (sockets.Count > 0)
                {
                    attrs.AddAttribute("SOCKETS", attr => attr.WriteArray(sockets, (writer, socket) =>
                    {
                        writer.WriteFString(socket.SocketName.Text);
                        writer.WriteFString(socket.BoneName.Text);
                        socket.RelativeLocation.Serialize(writer);
                        socket.RelativeRotation.Quaternion().Serialize(writer);
                        socket.RelativeScale.Serialize(writer);
                    }));
                }
            }

            if (skeleton.VirtualBones is { Length: > 0 })
            {
                attrs.AddAttribute("VIRTUALBONES", attr => attr.WriteArray(skeleton.VirtualBones, (writer, virtualBone) =>
                {
                    writer.WriteFString(virtualBone.SourceBoneName.Text);
                    writer.WriteFString(virtualBone.TargetBoneName.Text);
                    writer.WriteFString(virtualBone.VirtualBoneName.Text);
                }));
            }
        }));
    }

    private static void WriteCommonMesh<TVertex>(FDataAttributeSet attrs, MeshLodDto<TVertex> lod)
        where TVertex : struct, IMeshVertex
    {
        attrs.AddAttribute("VERTICES", attr => attr.WriteArray(lod.Vertices, (writer, vertex) =>
            vertex.Position.Serialize(writer)));

        attrs.AddAttribute("NORMALS", attr => attr.WriteArray(lod.Vertices, (writer, vertex) =>
        {
            writer.Write(vertex.Normal.W);
            var normal = (FVector) vertex.Normal;
            normal /= MathF.Sqrt(normal | normal);
            normal.Serialize(writer);
        }));

        attrs.AddAttribute("TANGENTS", attr => attr.WriteArray(lod.Vertices, (writer, vertex) =>
        {
            var tangent = (FVector) vertex.Tangent;
            tangent.Normalize();
            tangent.Serialize(writer);
        }));

        attrs.AddAttribute("TEXCOORDS", attr =>
        {
            var mainUvs = lod.Vertices.Select(v => v.Uv).ToArray();
            
            var uvSets = new List<(string Name, FMeshUVFloat[] Uvs)>(1 + lod.ExtraUvs.Length) { ("UV0", mainUvs) };
            uvSets.AddRange(lod.ExtraUvs.Select((t, i) => ($"UV{i + 1}", t)));

            attr.WriteArray(uvSets, (writer, uvSet) =>
            {
                writer.WriteFString(uvSet.Name);
                writer.WriteArray(uvSet.Uvs, (writer, uv) => uv.Serialize(writer));
            });
        });

        attrs.AddAttribute("INDICES", attr => attr.WriteArray(lod.Indices, (writer, index) => writer.Write(index)));

        if (lod.VertexColors is { Length: > 0 })
        {
            attrs.AddAttribute("VERTEXCOLORS", attr => attr.WriteArray(lod.VertexColors, (writer, colorSet) =>
            {
                writer.WriteFString(colorSet.Name);
                writer.WriteArray(colorSet.Colors, (writer, color) => color.Serialize(writer));
            }));
        }

        attrs.AddAttribute("MATERIALS", attr => attr.WriteArray(lod.Sections, (writer, section, i) =>
        {
            var material = lod.Owner.GetMaterial(section);
            writer.WriteFString(material?.SlotName ?? $"MaterialSlot_{i}");
            writer.WriteFString(material?.Material?.ResolvedObject?.GetPathName() ?? string.Empty);
            writer.Write(section.FirstIndex);
            writer.Write(section.NumFaces);
        }));
    }

    private static void WriteSkinning(FDataAttributeSet attrs, MeshLodDto<SkinnedMeshVertex> lod, FPackageIndex[]? morphTargets)
    {
        var weights = new List<(ushort Bone, int VertexIndex, float Weight)>();
        for (var vertexIndex = 0; vertexIndex < lod.Vertices.Length; vertexIndex++)
        {
            weights.AddRange(lod.Vertices[vertexIndex].Influences.Select(influence => (influence.Bone, vertexIndex, influence.Weight)));
        }

        if (weights.Count > 0)
        {
            attrs.AddAttribute("WEIGHTS", attr => attr.WriteArray(weights, (writer, weight) =>
            {
                writer.Write(weight.Bone);
                writer.Write(weight.VertexIndex);
                writer.Write(weight.Weight);
            }));
        }
        
        var morphList = new List<FMorphTarget>();
        foreach (var morphTarget in morphTargets ?? [])
        {
            var morph = morphTarget.Load<UMorphTarget>();
            if (morph?.MorphLODModels is null ||
                lod.SourceLodIndex >= morph.MorphLODModels.Length ||
                morph.MorphLODModels[lod.SourceLodIndex].Vertices.Length == 0)
                continue;

            morphList.Add(new FMorphTarget(morph.Name, morph.MorphLODModels[lod.SourceLodIndex]));
        }

        if (morphList.Count > 0)
        {
            attrs.AddAttribute("MORPHTARGETS", attr => attr.WriteArray(morphList));
        }
        
    }

    private static List<MeshLodDto<TVertex>> FilterLods<TVertex>(
        IList<MeshLodDto<TVertex>> lods,
        Func<MeshLodDto<TVertex>, bool>? predicate) where TVertex : struct, IMeshVertex
    {
        if (predicate is null)
            return lods as List<MeshLodDto<TVertex>> ?? [..lods];

        return lods.Where(predicate).ToList();
    }
}
