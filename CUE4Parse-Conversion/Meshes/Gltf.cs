using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Writers;
using CUE4Parse.Utils;
using CUE4Parse_Conversion.Materials;
using CUE4Parse_Conversion.Meshes.glTF;
using CUE4Parse_Conversion.Meshes.PSK;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using SharpGLTF.IO;

namespace CUE4Parse_Conversion.Meshes
{
    using VERTEX = VertexPositionNormalTangent;
    public class Gltf
    {
        public readonly ModelRoot Model;

        public Gltf(string name, CStaticMeshLod lod, List<MaterialExporter>? materialExports)
        {
            var mesh = new MeshBuilder<VERTEX, VertexColorXTextureX, VertexEmpty>(name);

            for (var i = 0; i < lod.Sections.Value.Length; i++)
            {
                ExportStaticMeshSections(i, lod, lod.Sections.Value[i], materialExports, mesh);
            }

            var scene = new SceneBuilder();
            scene.AddRigidMesh(mesh, Matrix4x4.Identity);
            Model = scene.ToGltf2();
        }

        public Gltf(string name, CSkelMeshLod lod, List<CSkelMeshBone> bones, List<MaterialExporter>? materialExports)
        {
            var mesh = new MeshBuilder<VERTEX, VertexColorXTextureX, VertexJoints4>(name);

            for (var i = 0; i < lod.Sections.Value.Length; i++)
            {
                ExportSkelMeshSections(i, lod, lod.Sections.Value[i], materialExports, mesh);
            }

            var sceneBuilder = new SceneBuilder();
            var armatureNodeBuilder = new NodeBuilder(name+".ao");

            var armature = CreateGltfSkeleton(bones, armatureNodeBuilder);
            sceneBuilder.AddSkinnedMesh(mesh, Matrix4x4.Identity, armature);

            Model = sceneBuilder.ToGltf2();
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

        public static NodeBuilder[] CreateGltfSkeleton(List<CSkelMeshBone> skeleton, NodeBuilder armatureNode) // TODO optimize
        {
            var result = new List<NodeBuilder>();

            for (var i = 0; i < skeleton.Count; i++)
            {
                var root = skeleton[i];
                if (root.ParentIndex != -1) continue;

                var rootCopy = (CSkelMeshBone)root.Clone(); // we don't want to modify the original skeleton
                rootCopy.Orientation = FQuat.Conjugate(root.Orientation);
                result.AddRange(CreateBonesRecursive(rootCopy, armatureNode, skeleton, i));
            }

            return result.ToArray();
        }

        private static List<NodeBuilder> CreateBonesRecursive(CSkelMeshBone bone, NodeBuilder parent, List<CSkelMeshBone> skeleton, int index)
        {
            var res = new List<NodeBuilder>();

            var bonePos = SwapYZ(bone.Position*0.01f);
            var boneRot = SwapYZ(bone.Orientation);
            var node = parent.CreateNode(bone.Name.ToString())
                .WithLocalRotation(boneRot.ToQuaternion())
                .WithLocalTranslation(bonePos);

            res.Add(node);

            var numBones = skeleton.Count;
            for (int j = 0; j < numBones; j++)
            {
                if (index == j) continue;
                var bone2 = skeleton[j];
                if (bone2.ParentIndex == index)
                {
                    res.AddRange(CreateBonesRecursive(bone2, node, skeleton, j));
                }
            }
            return res;
        }

        private static void ExportSkelMeshSections(int index, CSkelMeshLod lod, CMeshSection sect, List<MaterialExporter>? materialExports, MeshBuilder<VERTEX, VertexColorXTextureX, VertexJoints4> mesh)
        {
            string materialName;
            if (sect.Material?.Load<UMaterialInterface>() is { } tex)
            {
                materialName = tex.Name;
                var materialExporter = new MaterialExporter(tex, true);
                materialExports?.Add(materialExporter);
            }
            else materialName = $"material_{index}";

            var mat = new MaterialBuilder().WithBaseColor(Vector4.One).WithDoubleSide(true);
            mat.Name = materialName;

            var prim = mesh.UsePrimitive(mat);
            for (int j = 0; j < sect.NumFaces; j++)
            {
                var wedgeIndex = new int[3];
                for (var k = 0; k < wedgeIndex.Length; k++)
                {
                    wedgeIndex[k] = lod.Indices.Value[sect.FirstIndex + j * 3 + k];
                }

                var vert1 = lod.Verts[wedgeIndex[0]];
                var vert2 = lod.Verts[wedgeIndex[1]];
                var vert3 = lod.Verts[wedgeIndex[2]];

                var (v1, v2, v3) = PrepareTris(vert1, vert2, vert3);
                var (c1, c2, c3) = PrepareUVsAndTexCoords(lod, vert1, vert2, vert3, index);

                var (jv1, jv2, jv3) = PrepareVertexJoints(vert1, vert2, vert3);

                prim.AddTriangle((v1, c1, jv1), (v2, c2, jv2), (v3, c3, jv3));
            }
        }

        private static void ExportStaticMeshSections(int index, CStaticMeshLod lod, CMeshSection sect, List<MaterialExporter>? materialExports, MeshBuilder<VERTEX, VertexColorXTextureX, VertexEmpty> mesh)
        {
            string materialName;
            if (sect.Material?.Load<UMaterialInterface>() is { } tex)
            {
                materialName = tex.Name;
                var materialExporter = new MaterialExporter(tex, true);
                materialExports?.Add(materialExporter);
            }
            else materialName = $"material_{index}";

            var mat = new MaterialBuilder().WithBaseColor(Vector4.One).WithDoubleSide(true);
            mat.Name = materialName;

            var prim = mesh.UsePrimitive(mat);
            for (int j = 0; j < sect.NumFaces; j++)
            {
                var wedgeIndex = new int[3];
                for (var k = 0; k < wedgeIndex.Length; k++)
                {
                    wedgeIndex[k] = lod.Indices.Value[sect.FirstIndex + j * 3 + k];
                }

                var vert1 = lod.Verts[wedgeIndex[0]];
                var vert2 = lod.Verts[wedgeIndex[1]];
                var vert3 = lod.Verts[wedgeIndex[2]];

                var (v1, v2, v3) = PrepareTris(vert1, vert2, vert3);
                var (c1, c2, c3) = PrepareUVsAndTexCoords(lod, vert1, vert2, vert3, index);

                prim.AddTriangle((v1, c1), (v2, c2), (v3, c3));
            }
        }

        public static VertexJoints4 PrepareVertexJoint(CSkelMeshVertex vert)
        {
            var weights = vert.UnpackWeights();
            var j1 = new List<(int, float)>();

            if (weights.All((v) => v == 0) || vert.Bone == null)
                return new VertexJoints4(j1.ToArray());

            for (int i = 0; i < vert.Bone.Length; i++)
            {
                j1.Add((vert.Bone[i], weights[i]));
            }

            return new VertexJoints4(j1.ToArray());
        }

        public static (VertexJoints4, VertexJoints4, VertexJoints4) PrepareVertexJoints(CSkelMeshVertex vert1, CSkelMeshVertex vert2, CSkelMeshVertex vert3)
        {
            var jv1 = PrepareVertexJoint(vert1);
            var jv2 = PrepareVertexJoint(vert2);
            var jv3 = PrepareVertexJoint(vert3);

            return (jv1, jv2, jv3);
        }

        public static (VertexColorXTextureX, VertexColorXTextureX, VertexColorXTextureX) PrepareUVsAndTexCoords(
            CBaseMeshLod lod, CMeshVertex vert1, CMeshVertex vert2, CMeshVertex vert3, int firstIndex)
        {
            return PrepareUVsAndTexCoords(lod.VertexColors ?? new FColor[lod.NumVerts], vert1, vert2, vert3,
                lod.ExtraUV.Value, firstIndex);
        }

        public static (VertexColorXTextureX, VertexColorXTextureX, VertexColorXTextureX) PrepareUVsAndTexCoords(
            FColor[] colors, CMeshVertex vert1, CMeshVertex vert2, CMeshVertex vert3, FMeshUVFloat[][] uvs, int firstIndex)
        {
            var (uvs1, uvs2, uvs3) = PrepareUVs(vert1, vert2, vert3, uvs, firstIndex);
            var c1 = new VertexColorXTextureX(colors[firstIndex], uvs1);
            var c2 = new VertexColorXTextureX(colors[firstIndex+1], uvs2);
            var c3 = new VertexColorXTextureX(colors[firstIndex+2], uvs3);
            return (c1, c2, c3);
        }

        private static (List<Vector2>, List<Vector2>, List<Vector2>) PrepareUVs(CMeshVertex vert1, CMeshVertex vert2, CMeshVertex vert3, FMeshUVFloat[][] uvs, int firstIndex)
        {
            var uvs1 = new List<Vector2>() { vert1.UV };
            var uvs2 = new List<Vector2>() { vert2.UV };
            var uvs3 = new List<Vector2>() { vert3.UV };
            foreach (var uv in uvs)
            {
                uvs1.Add(uv[firstIndex]);
                uvs2.Add(uv[firstIndex+1]);
                uvs3.Add(uv[firstIndex+2]);
            }

            return (uvs1, uvs2, uvs3);
        }

        private static (VERTEX, VERTEX, VERTEX) PrepareTris(CMeshVertex vert1, CMeshVertex vert2, CMeshVertex vert3)
        {
            var v1 = new VertexPositionNormalTangent(SwapYZ(vert1.Position*0.01f),SwapYZAndNormalize((FVector)vert1.Normal) , SwapYZAndNormalize((Vector4)vert1.Tangent));
            var v2 = new VertexPositionNormalTangent(SwapYZ(vert2.Position*0.01f), SwapYZAndNormalize((FVector)vert2.Normal), SwapYZAndNormalize((Vector4)vert2.Tangent));
            var v3 = new VertexPositionNormalTangent(SwapYZ(vert3.Position*0.01f), SwapYZAndNormalize((FVector)vert3.Normal), SwapYZAndNormalize((Vector4)vert3.Tangent));

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

        public static FQuat SwapYZ(FQuat quat) => new FQuat(quat.X, quat.Z, quat.Y, quat.W);

        public static Vector4 SwapYZAndNormalize(Vector4 vec)
        {
          return Vector4.Normalize(new Vector4(vec.X, vec.Z, vec.Y, vec.W));
        }
    }
}