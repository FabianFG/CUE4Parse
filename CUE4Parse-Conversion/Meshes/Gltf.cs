using System.Collections.Generic;
using System.Numerics;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.RenderCore;
using CUE4Parse.UE4.Writers;
using CUE4Parse_Conversion.Materials;
using CUE4Parse_Conversion.Meshes.glTF;
using CUE4Parse_Conversion.Meshes.PSK;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;


namespace CUE4Parse_Conversion.Meshes
{
    public class Gltf
    {
        public Gltf(string name, CStaticMeshLod lod, FArchiveWriter Ar, List<MaterialExporter>? materialExports)
        {
            var mesh = new MeshBuilder<VertexPositionNormalTangent, VertexColorXTextureX, VertexEmpty>(name);
            var numFaces = 0;
            for (var i = 0; i < lod.Sections.Value.Length; i++)
            {
                var sect = lod.Sections.Value[i];

                string materialName;
                MaterialExporter materialExporter = null;
                if (sect.Material?.Load<UMaterialInterface>() is { } tex)
                {
                    materialName = tex.Name;
                    materialExporter = new MaterialExporter(tex, true);
                    materialExports?.Add(materialExporter);
                }
                else materialName = $"material_{i}";

                var mat = new MaterialBuilder().WithBaseColor(Vector4.One).WithDoubleSide(true);
                mat.Name = materialName;

                var prim = mesh.UsePrimitive(mat);
                for (int j = 0; j < sect.NumFaces; j++)
                {
                    var wedgeIndex = new int[3];
                    for (var k = 0; k < wedgeIndex.Length; k++)
                    {
                        wedgeIndex[k] = (int) lod.Indices.Value[sect.FirstIndex + j * 3 + k];
                    }

                    var tri1 = lod.Verts[wedgeIndex[0]];
                    var tri2 = lod.Verts[wedgeIndex[1]];
                    var tri3 = lod.Verts[wedgeIndex[2]];

                    // face verts
                    var v1 = new VertexPositionNormalTangent(SwapYZAndNormalize(tri1.Position*0.01f),SwapYZAndNormalize((FVector)tri1.Normal) , SwapYZAndNormalize((Vector4)tri1.Tangent));
                    var v2 = new VertexPositionNormalTangent(SwapYZAndNormalize(tri2.Position*0.01f), SwapYZAndNormalize((FVector)tri2.Normal), SwapYZAndNormalize((Vector4)tri2.Tangent));
                    var v3 = new VertexPositionNormalTangent(SwapYZAndNormalize(tri3.Position*0.01f), SwapYZAndNormalize((FVector)tri3.Normal), SwapYZAndNormalize((Vector4)tri3.Tangent));

                    var uvs1 = new List<Vector2>() { tri1.UV };
                    var uvs2 = new List<Vector2>() { tri2.UV };
                    var uvs3 = new List<Vector2>() { tri3.UV };
                    foreach (var uv in lod.ExtraUV.Value)
                    {
                        uvs1.Add(uv[wedgeIndex[0]]);
                        uvs2.Add(uv[wedgeIndex[1]]);
                        uvs3.Add(uv[wedgeIndex[2]]);
                    }

                    var c1 = new VertexColorXTextureX(lod.VertexColors[wedgeIndex[0]], uvs1);
                    var c2 = new VertexColorXTextureX(lod.VertexColors[wedgeIndex[1]], uvs2);
                    var c3 = new VertexColorXTextureX(lod.VertexColors[wedgeIndex[2]], uvs3);
                    prim.AddTriangle((v1, c1), (v2, c2), (v3, c3));
                }
                numFaces += sect.NumFaces;
            }

            var scene = new SceneBuilder();
            scene.AddRigidMesh(mesh, Matrix4x4.Identity);

            var model = scene.ToGltf2();
            Ar.Write(model.WriteGLB());
        }

        public static FVector SwapYZAndNormalize(FVector vec)
        {
            var res = new FVector(vec.X, vec.Z, vec.Y);
            res.Normalize();
            return res;
        }

        public static Vector4 SwapYZAndNormalize(Vector4 vec)
        {
          return Vector4.Normalize(new Vector4(vec.X, vec.Z, vec.Y, vec.W));
        }
    }
}
