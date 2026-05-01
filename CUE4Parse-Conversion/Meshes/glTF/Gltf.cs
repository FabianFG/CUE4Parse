using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CUE4Parse_Conversion.V2.Dto;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Writers;
using CUE4Parse.Utils;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.IO;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;

namespace CUE4Parse_Conversion.Meshes.glTF
{
    using VERTEX = VertexPositionNormalTangent;
    public class Gltf
    {
        private const float UnitScale = 0.01f;

        public readonly ModelRoot Model;

        public Gltf(string name, StaticMesh mesh, ExporterOptions options)
        {
            var sceneBuilder = new SceneBuilder(name);
            var origin = mesh.Bounds.GetExtent().Y * 2 * UnitScale;

            for (var lodIdx = 0; lodIdx < mesh.LODs.Count; lodIdx++)
            {
                var lod = mesh.LODs[lodIdx];
                var offsetZ = origin * lodIdx;

                var meshBuilder = new MeshBuilder<VERTEX, VertexColorXTextureX, VertexEmpty>($"LOD{lodIdx}");
                ExportMeshSections(meshBuilder, lod);
                sceneBuilder.AddRigidMesh(meshBuilder, Matrix4x4.CreateTranslation(0, 0, offsetZ));

                if (options.LodFormat == ELodFormat.FirstLod) break;
            }

            Model = sceneBuilder.ToGltf2();
        }

        public Gltf(string name, SkeletalMesh mesh, ExporterOptions options)
        {
            var sceneBuilder = new SceneBuilder(name);
            var origin = mesh.Bounds.GetExtent().Y * 2 * UnitScale;

            for (var lodIdx = 0; lodIdx < mesh.LODs.Count; lodIdx++)
            {
                var offsetZ = origin * lodIdx;
                var armatureRoot = new NodeBuilder($"{name}.ao_LOD{lodIdx}").WithLocalTranslation(new Vector3(0, 0, offsetZ));
                var armature = CreateGltfSkeleton(mesh.Bones, armatureRoot);

                var lod = mesh.LODs[lodIdx];
                var meshBuilder = new MeshBuilder<VERTEX, VertexColorXTextureX, VertexJoints4>($"LOD{lodIdx}");
                ExportMeshSections(meshBuilder, lod);
                sceneBuilder.AddSkinnedMesh(meshBuilder, Matrix4x4.CreateTranslation(0, 0, offsetZ), armature);

                if (mesh.MorphTargets is { Length: > 0 } morphTargets)
                {
                    var targetNames = "{\"targetNames\": [";
                    for (var i = 0; i < morphTargets.Length; i++)
                    {
                        var morphTarget = morphTargets[i].Load<UMorphTarget>();
                        if (morphTarget?.MorphLODModels == null || morphTarget.MorphLODModels.Length < lodIdx || lodIdx == -1)
                            continue;
                        var morphBuilder = meshBuilder.UseMorphTarget(i);
                        var morphModel = morphTarget.MorphLODModels[lodIdx];

                        targetNames += $"\"{morphTarget.Name}\"";
                        targetNames += i != morphTargets.Length-1 ? "," : "";

                        var verts = morphBuilder.Vertices.ToArray();
                        for (int j = 0; j < morphModel.Vertices.Length; j++) // morphModel.NumBaseMeshVerts can be different from verts.Length
                        {
                            var delta = morphModel.Vertices[j];
                            var vert = lod.Vertices[delta.SourceIdx];
                            var srcVert = new VertexPositionNormalTangent(SwapYZ(vert.Position * UnitScale),SwapYZAndNormalize((FVector)vert.Normal) , SwapYZAndNormalize((Vector4)vert.Tangent));
                            var index = FindVert(srcVert, verts);
                            if (index == -1)  continue;

                            morphBuilder.SetVertexDelta(morphBuilder.Vertices.ElementAt(index), new VertexGeometryDelta(SwapYZ(delta.PositionDelta * UnitScale), Vector3.Zero, SwapYZAndNormalize(delta.TangentZDelta)));
                        }
                    }

                    targetNames += "]}";
                    meshBuilder.Extras = (JsonContent) targetNames;
                }

                if (options.LodFormat == ELodFormat.FirstLod) break;
            }

            Model = sceneBuilder.ToGltf2();
        }

        private static int FindVert(VertexPositionNormalTangent a, VertexPositionNormalTangent[] b)
        {
            for (int i = 0; i < b.Length; i++)
            {
                if (b[i].GetPosition() == a.GetPosition()) // not a good idea but i don't see any other way
                    return i;
            }
            return -1;
        }

        public ArraySegment<byte> SaveAsWavefront()
        {
            throw new NotImplementedException();
        }

        public void Save(EMeshFormat meshFormat, FArchiveWriter Ar)
        {
            switch (meshFormat)
            {
                case EMeshFormat.Gltf2:
                    Ar.Write(Model.WriteGLB());
                    break;
                case EMeshFormat.OBJ:
                    Ar.Write(SaveAsWavefront()); // this can be supported after new release of SharpGltf
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(meshFormat), meshFormat, null);
            }
        }

        public static NodeBuilder[] CreateGltfSkeleton(IReadOnlyList<MeshBone> bones, NodeBuilder armatureNode)
        {
            var result = new NodeBuilder[bones.Count];

            // build children lookup in one pass — O(N) instead of O(N²) recursive scan
            var children = new List<int>[bones.Count];
            for (var i = 0; i < bones.Count; i++) children[i] = [];
            for (var i = 0; i < bones.Count; i++)
            {
                var p = bones[i].ParentIndex;
                if (p >= 0 && p < bones.Count) children[p].Add(i);
            }

            // process root bones (parentIndex == -1)
            for (var i = 0; i < bones.Count; i++)
            {
                if (bones[i].ParentIndex != -1) continue;

                // iterative DFS — avoids stack overflow on deep hierarchies
                var stack = new Stack<(int index, NodeBuilder parent)>();
                stack.Push((i, armatureNode));
                while (stack.Count > 0)
                {
                    var (idx, parentNode) = stack.Pop();
                    var bone = bones[idx];
                    var node = parentNode
                        .CreateNode(bone.Name)
                        .WithLocalRotation(SwapYZ(bone.Transform.Rotation).ToQuaternion())
                        .WithLocalTranslation(SwapYZ(bone.Transform.Translation * UnitScale))
                        .WithLocalScale(SwapYZ(bone.Transform.Scale3D));
                    result[idx] = node;
                    foreach (var childIdx in children[idx])
                        stack.Push((childIdx, node));
                }
            }

            return result;
        }


        private void ExportMeshSections<TVertex>(IMeshBuilder<MaterialBuilder> builder, MeshLod<TVertex> lod) where TVertex : struct, IMeshVertex
        {
            FColor[]? colors = null;
            if (lod.VertexColors is { Length: > 0 })
                colors = lod.VertexColors[0].Colors;

            var uvCount = 1 + lod.ExtraUvs.Length;
            var uvList1 = new Vector2[uvCount];
            var uvList2 = new Vector2[uvCount];
            var uvList3 = new Vector2[uvCount];

            var isSkinned = typeof(TVertex) == typeof(SkinnedMeshVertex);

            for (var i = 0; i < lod.Sections.Length; i++)
            {
                var section = lod.Sections[i];
                var mat = new MaterialBuilder().WithBaseColor(Vector4.One);
                mat.Name = lod.Owner.GetMaterial(section)?.SlotName ?? $"MaterialSlot_{i}";

                var prim = builder.UsePrimitive(mat);
                for (var j = 0; j < section.NumFaces; j++)
                {
                    var idx0 = lod.Indices[section.FirstIndex + j * 3 + 0];
                    var idx1 = lod.Indices[section.FirstIndex + j * 3 + 1];
                    var idx2 = lod.Indices[section.FirstIndex + j * 3 + 2];

                    var vert1 = lod.Vertices[idx0];
                    var vert2 = lod.Vertices[idx1];
                    var vert3 = lod.Vertices[idx2];

                    var (v1, v2, v3) = PrepareTris(vert1, vert2, vert3);

                    // fill shared UV lists in-place (no per-triangle allocation)
                    uvList1[0] = (Vector2)vert1.Uv;
                    uvList2[0] = (Vector2)vert2.Uv;
                    uvList3[0] = (Vector2)vert3.Uv;
                    for (var k = 0; k < lod.ExtraUvs.Length; k++)
                    {
                        uvList1[k + 1] = (Vector2)lod.ExtraUvs[k][idx0];
                        uvList2[k + 1] = (Vector2)lod.ExtraUvs[k][idx1];
                        uvList3[k + 1] = (Vector2)lod.ExtraUvs[k][idx2];
                    }

                    var c1 = new VertexColorXTextureX(uvList1, colors?[idx0]);
                    var c2 = new VertexColorXTextureX(uvList2, colors?[idx1]);
                    var c3 = new VertexColorXTextureX(uvList3, colors?[idx2]);

                    IVertexBuilder a, b, c;
                    if (isSkinned && vert1 is SkinnedMeshVertex j1 && vert2 is SkinnedMeshVertex j2 && vert3 is SkinnedMeshVertex j3)
                    {
                        var (jv1, jv2, jv3) = PrepareVertexJoints(j1, j2, j3);
                        a = new VertexBuilder<VERTEX, VertexColorXTextureX, VertexJoints4>(v1, c1, jv1);
                        b = new VertexBuilder<VERTEX, VertexColorXTextureX, VertexJoints4>(v2, c2, jv2);
                        c = new VertexBuilder<VERTEX, VertexColorXTextureX, VertexJoints4>(v3, c3, jv3);
                    }
                    else
                    {
                        a = new VertexBuilder<VERTEX, VertexColorXTextureX, VertexEmpty>(v1, c1);
                        b = new VertexBuilder<VERTEX, VertexColorXTextureX, VertexEmpty>(v2, c2);
                        c = new VertexBuilder<VERTEX, VertexColorXTextureX, VertexEmpty>(v3, c3);
                    }

                    prim.AddTriangle(a, b, c);
                }
            }
        }

        public static VertexJoints4 PrepareVertexJoint(SkinnedMeshVertex vert)
        {
            var bindings = new (int, float)[vert.Influences.Length];
            for (var i = 0; i < bindings.Length; i++)
            {
                bindings[i] = (vert.Influences[i].Bone, vert.Influences[i].Weight);
            }
            return new VertexJoints4(bindings);
        }

        public static (VertexJoints4, VertexJoints4, VertexJoints4) PrepareVertexJoints(SkinnedMeshVertex vert1, SkinnedMeshVertex vert2, SkinnedMeshVertex vert3)
        {
            var jv1 = PrepareVertexJoint(vert1);
            var jv2 = PrepareVertexJoint(vert2);
            var jv3 = PrepareVertexJoint(vert3);

            return (jv1, jv2, jv3);
        }

        private static (VERTEX, VERTEX, VERTEX) PrepareTris(IMeshVertex vert1, IMeshVertex vert2, IMeshVertex vert3)
        {
            var v1 = new VertexPositionNormalTangent(SwapYZ(vert1.Position * UnitScale),SwapYZAndNormalize((FVector)vert1.Normal) , SwapYZAndNormalize((Vector4)vert1.Tangent));
            var v2 = new VertexPositionNormalTangent(SwapYZ(vert2.Position * UnitScale), SwapYZAndNormalize((FVector)vert2.Normal), SwapYZAndNormalize((Vector4)vert2.Tangent));
            var v3 = new VertexPositionNormalTangent(SwapYZ(vert3.Position * UnitScale), SwapYZAndNormalize((FVector)vert3.Normal), SwapYZAndNormalize((Vector4)vert3.Tangent));

            return (v1, v2, v3);
        }

        public static FVector SwapYZAndNormalize(FVector vec)
        {
            var res = SwapYZ(vec);
            res.Normalize();
            return res;
        }

        public static FVector SwapYZ(FVector vec)
        {
            var res = new FVector(vec.X, vec.Z, vec.Y);
            return res;
        }

        public static FQuat SwapYZ(FQuat quat) => new (quat.X, quat.Z, quat.Y, -quat.W);

        public static Vector4 SwapYZAndNormalize(Vector4 vec)
        {
          return Vector4.Normalize(new Vector4(vec.X, vec.Z, vec.Y, vec.W));
        }
    }
}
