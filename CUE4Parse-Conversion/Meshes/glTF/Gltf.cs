using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CUE4Parse_Conversion.V2.Dto;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
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
                var armature = CreateGltfSkeleton(mesh.RefSkeleton, armatureRoot);

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

        public static NodeBuilder[] CreateGltfSkeleton(IReadOnlyList<MeshBone> bones, NodeBuilder armatureNode) // TODO optimize
        {
            var result = new NodeBuilder[bones.Count];

            for (var i = 0; i < bones.Count; i++)
            {
                var bone = bones[i];
                if (bone.ParentIndex != -1) continue;

                CreateBonesRecursive(bone, armatureNode, bones, i, result);
            }

            return result;
        }

        private static void CreateBonesRecursive(MeshBone bone, NodeBuilder parent, IReadOnlyList<MeshBone> bones, int index, NodeBuilder[] result)
        {
            var bonePos = SwapYZ(bone.Transform.Translation * UnitScale);
            var boneRot = SwapYZ(bone.Transform.Rotation);
            var boneSca = SwapYZ(bone.Transform.Scale3D);
            var node = parent.CreateNode(bone.Name).WithLocalRotation(boneRot.ToQuaternion()).WithLocalTranslation(bonePos).WithLocalScale(boneSca);

            result[index] = node;

            for (int j = 0; j < bones.Count; j++)
            {
                if (index == j) continue;
                var bone2 = bones[j];
                if (bone2.ParentIndex == index)
                {
                    CreateBonesRecursive(bone2, node, bones, j, result);
                }
            }
        }

        private void ExportMeshSections<TVertex>(IMeshBuilder<MaterialBuilder> builder, MeshLod<TVertex> lod) where TVertex : struct, IMeshVertex
        {
            for (var i = 0; i < lod.Sections.Length; i++)
            {
                var section = lod.Sections[i];
                var mat = new MaterialBuilder().WithBaseColor(Vector4.One);
                mat.Name = lod.Owner.GetMaterial(section)?.SlotName ?? $"MaterialSlot_{i}";

                var prim = builder.UsePrimitive(mat);
                for (var j = 0; j < section.NumFaces; j++)
                {
                    var wedgeIndex = new uint[3];
                    for (var k = 0; k < wedgeIndex.Length; k++)
                    {
                        wedgeIndex[k] = lod.Indices[section.FirstIndex + j * 3 + k];
                    }

                    var vert1 = lod.Vertices[wedgeIndex[0]];
                    var vert2 = lod.Vertices[wedgeIndex[1]];
                    var vert3 = lod.Vertices[wedgeIndex[2]];

                    var (v1, v2, v3) = PrepareTris(vert1, vert2, vert3);
                    var (c1, c2, c3) = PrepareUVsAndTexCoords(lod, vert1, vert2, vert3, wedgeIndex);

                    IVertexBuilder a, b, c;
                    if (vert1 is SkinnedMeshVertex j1 && vert2 is SkinnedMeshVertex j2 && vert3 is SkinnedMeshVertex j3)
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
            var bindings = new List<(int, float)>();

            foreach (var influence in vert.Influences)
            {
                bindings.Add((influence.Bone, influence.Weight));
            }

            return new VertexJoints4(bindings.ToArray());
        }

        public static (VertexJoints4, VertexJoints4, VertexJoints4) PrepareVertexJoints(SkinnedMeshVertex vert1, SkinnedMeshVertex vert2, SkinnedMeshVertex vert3)
        {
            var jv1 = PrepareVertexJoint(vert1);
            var jv2 = PrepareVertexJoint(vert2);
            var jv3 = PrepareVertexJoint(vert3);

            return (jv1, jv2, jv3);
        }

        public static (VertexColorXTextureX, VertexColorXTextureX, VertexColorXTextureX) PrepareUVsAndTexCoords<TVertex>(
            MeshLod<TVertex> lod, IMeshVertex vert1, IMeshVertex vert2, IMeshVertex vert3, uint[] indices) where TVertex : struct, IMeshVertex
        {
            if (lod.VertexColors == null || !lod.VertexColors.TryGetValue("COL0", out var colors))
            {
                colors = new FColor[lod.Vertices.Length];
            }

            return PrepareUVsAndTexCoords(colors, vert1, vert2, vert3, lod.ExtraUvs, indices);
        }

        public static (VertexColorXTextureX, VertexColorXTextureX, VertexColorXTextureX) PrepareUVsAndTexCoords(
            FColor[] colors, IMeshVertex vert1, IMeshVertex vert2, IMeshVertex vert3, FMeshUVFloat[][] uvs, uint[] indices)
        {
            var (uvs1, uvs2, uvs3) = PrepareUVs(vert1, vert2, vert3, uvs, indices);
            var c1 = new VertexColorXTextureX(colors[indices[0]], uvs1);
            var c2 = new VertexColorXTextureX(colors[indices[1]], uvs2);
            var c3 = new VertexColorXTextureX(colors[indices[2]], uvs3);
            return (c1, c2, c3);
        }

        private static (List<Vector2>, List<Vector2>, List<Vector2>) PrepareUVs(IMeshVertex vert1, IMeshVertex vert2, IMeshVertex vert3, FMeshUVFloat[][] uvs, uint[] indices)
        {
            var uvs1 = new List<Vector2>() { (Vector2)vert1.Uv };
            var uvs2 = new List<Vector2>() { (Vector2)vert2.Uv };
            var uvs3 = new List<Vector2>() { (Vector2)vert3.Uv };
            foreach (var uv in uvs)
            {
                uvs1.Add((Vector2)uv[indices[0]]);
                uvs2.Add((Vector2)uv[indices[1]]);
                uvs3.Add((Vector2)uv[indices[2]]);
            }

            return (uvs1, uvs2, uvs3);
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
