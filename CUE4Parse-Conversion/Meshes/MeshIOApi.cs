using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AssetRipper.MeshSharp;
using AssetRipper.MeshSharp.Elements;
using AssetRipper.MeshSharp.Elements.Geometries.Layers;
using AssetRipper.MeshSharp.FBX;
using CUE4Parse_Conversion.Materials;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Meshes;

public class MeshIOApi
{
    public readonly Scene MeshIOScene;

    public MeshIOApi(string name, CStaticMeshLod lod, List<MaterialExporter2>? materialExports, ExporterOptions options)
    {
        MeshIOScene = new Scene("MyScene"); // unused

        var meshNode = ExportStaticMesh(name, lod, lod.Sections.Value, materialExports, options);
        MeshIOScene.Nodes.Add(meshNode);
    }

    public void Save(EMeshFormat meshFormat, FArchiveWriter Ar)
    {
        if (meshFormat == EMeshFormat.FBX)
        {
            new FbxWriter(Ar.GetStream(), MeshIOScene, FbxVersion.v7500).WriteAscii();
            return;
        }
        throw new ArgumentOutOfRangeException();
    }

    public static Node ExportStaticMesh(string name, CStaticMeshLod lod, CMeshSection[] sects, List<MaterialExporter2>? materialExports, ExporterOptions options)
    {
        var meshNode = new Node(name);
        var geometry = new AssetRipper.MeshSharp.Elements.Geometries.Mesh(name);
        geometry.Layers.Add(new List<LayerElement>());
        meshNode.Children.Add(geometry);

        (int[] vertRemap, int[] uniqueVerts) = DetermineVertsToWeld(lod, !options.WeldVerts);

        var numSections = sects.Length; // NumPolygons
        var vertexCount = vertRemap.Length;

        var meshVert = new XYZ[uniqueVerts.Length];
        // verts = ControlPoints (in fbx)
        for (int i = 0; i < uniqueVerts.Length; i++)
        {
            var vertIndex = uniqueVerts[i];
            var vert = lod.Verts[vertIndex];
            meshVert[i] = CastToXYZ(vert.Position*0.01f);
        }
        geometry.Vertices.AddRange(meshVert);

        var matLayer = new LayerElementMaterial(geometry)
        {
            Name = "",
            MappingInformationType = MappingMode.ByPolygon, ReferenceInformationType = ReferenceMode.IndexToDirect
        };
        geometry.Layers[0].Add(matLayer);

        var indices = new List<int>();
        for (int sectIndex = 0; sectIndex < numSections; sectIndex++)
        {
            var sect = sects[sectIndex];

            string materialName;
            MaterialExporter2 materialExporter = null;
            if (sect.Material?.Load<UMaterialInterface>() is { } tex)
            {
                materialName = tex.Name;
                materialExporter = new MaterialExporter2(tex, options);
                materialExports?.Add(materialExporter);
            }
            else materialName = $"material_{sectIndex}";

            var material = new Material(materialName) { ShadingModel = "phong"};
            meshNode.Children.Add(material);

            for (int faceIndex = 0; faceIndex < sect.NumFaces; faceIndex++)
            {
                var wedgeIndex = new int[3];
                for (var pointIndex = 0; pointIndex < 3; pointIndex++)
                {
                    var index = lod.Indices.Value[sect.FirstIndex + ((faceIndex * 3) + pointIndex)];
                    var reMappedIndex = vertRemap[index];
                    wedgeIndex[pointIndex] = reMappedIndex;

                    indices.Add(index);
                }
                matLayer.Materials.Add(sectIndex);
                geometry.Polygons.Add(new Triangle((uint)wedgeIndex[0], (uint)wedgeIndex[1], (uint)wedgeIndex[2]));
            }
        }

        // normal layer needs to be first?
        var normalLayer = new LayerElementNormal(geometry) { MappingInformationType = MappingMode.ByPolygonVertex, ReferenceInformationType = ReferenceMode.Direct };
        var tangentLayer = new LayerElementTangent(geometry) { MappingInformationType = MappingMode.ByPolygonVertex, ReferenceInformationType = ReferenceMode.Direct };
        // var binormalLayer = new LayerElementBinormal() { MappingMode = MappingMode.ByPolygonVertex, ReferenceMode = ReferenceMode.Direct }; // we don't have this i think

        // todo can this be done in single loop?
        var convertedNormals = new XYZ[vertexCount];
        var convertedTangents = new XYZ[vertexCount];
        // var convertedBinormals = new XYZ[vertexCount];
        for (int i = 0; i < vertexCount; i++)
        {
            var vert = lod.Verts[i];
            convertedNormals[i] = CastToXYZNormalize((FVector) vert.Normal);
            convertedTangents[i] = CastToXYZNormalize((FVector) vert.Tangent);
        }

        for (int i = 0; i < indices.Count; i++)
        {
            var index = indices[i];
            normalLayer.Normals.Add(convertedNormals[index]);
            tangentLayer.Tangents.Add(convertedTangents[index]);
        }

        geometry.Layers[0].Add(normalLayer);
        geometry.Layers[0].Add(tangentLayer);
        // mesh.Layers.Add(binormalLayer);

        // TODO: test Multiple UVs
        var numTextCoords = lod.NumTexCoords;
        for (int i = 0; i < numTextCoords; i++)
        {
            var uvChannelName = $"UVmap_{i}";
            var uvLayer = new LayerElementUV(geometry) { Name = uvChannelName, MappingInformationType = MappingMode.ByPolygonVertex, ReferenceInformationType = ReferenceMode.IndexToDirect };

            (int[] uvsRemap, int[] uniqueUVs) = DetermineUVsToWeld(lod.Verts, i, !options.WeldVerts);

            for (int j = 0; j < uniqueUVs.Length; j++)
            {
                int index = uniqueUVs[j];
                var uvFloat = i == 0 ? lod.Verts[index].UV : lod.ExtraUV.Value[i-1][index];
                uvLayer.UV.Add(new XY(uvFloat.U, -uvFloat.V+1.0));
            }

            uvLayer.UVIndex.AddRange(Enumerable.Repeat(new int(), indices.Count).ToList());
            for (int j = 0; j < indices.Count; j++)
            {
                uvLayer.UVIndex[j] = uvsRemap[indices[j]];
            }

            if (geometry.Layers.Count <= i)
                geometry.Layers.Add(new List<LayerElement>());
            geometry.Layers[i].Add(uvLayer);
        }

        if (lod.VertexColors != null)
        {
            var vertexColorLayer = new LayerElementVertexColor(geometry) { MappingInformationType = MappingMode.ByPolygonVertex, ReferenceInformationType = ReferenceMode.IndexToDirect };
            for (int j = 0; j < indices.Count; j++)
            {
                int index = indices[j];
                var vertColor = new FLinearColor(1, 1, 1, 1 );
                if (index < lod.VertexColors.Length)
                {
                    vertColor = lod.VertexColors[index].AsLinear();
                }

                vertexColorLayer.Colors.Add(new XYZM(vertColor.R, vertColor.G, vertColor.B, vertColor.A));
                vertexColorLayer.ColorIndex.Add(j);
            }
            geometry.Layers[0].Add(vertexColorLayer);
        }
        return meshNode;
    }

    private static XYZ CastToXYZ(FVector vec) => new XYZ(vec.X, vec.Z, vec.Y);
    private static XY CastToXY(Vector2 vec) => new XY(vec.X, vec.Y);
    private static XYZ CastToXYZNormalize(FVector vec)
    {
        vec = Gltf.SwapYZAndNormalize(vec);
        return new XYZ(vec.X, vec.Y, vec.Z);
    }

    public static (int[] VertRemap, int[] UniqueVerts) DetermineVertsToWeld(CStaticMeshLod lod, bool dontDoIt = false)
    {
        var vertCount = lod.Verts.Length;
        var vertRemap = new int[vertCount];
        var uniqueVerts = new List<int>();

        if (dontDoIt)
        {
            for (int i = 0; i < vertCount; i++)
            {
                uniqueVerts.Add(i);
                var index = uniqueVerts.Count - 1;
                vertRemap[i] = index;
            }
        }
        else
        {
            var hashedVerts = new Dictionary<FVector, int>();
            for(int i=0; i < vertCount; i++)
            {
                var PositionA = lod.Verts[i].Position;
                bool bFound = hashedVerts.Keys.Contains(PositionA); // (PositionA);
                if ( !bFound )
                {
                    uniqueVerts.Add(i);
                    var index = uniqueVerts.Count - 1;
                    vertRemap[i] = index;
                    hashedVerts.Add(PositionA, index);
                }
                else
                {
                    vertRemap[i] = hashedVerts[PositionA];
                }
            }
        }

        return (vertRemap, uniqueVerts.ToArray());
    }

    public static (int[] VertRemap, int[] UniqueVerts) DetermineUVsToWeld(CMeshVertex[] verts, int texIndex, bool dontDoIt = false)
    {
        var vertCount = verts.Length;
        var vertRemap = new int[vertCount];
        var uniqueVerts = new List<int>();

        if (dontDoIt)
        {
            for (int i = 0; i < vertCount; i++)
            {
                uniqueVerts.Add(i);
                var index = uniqueVerts.Count - 1;
                vertRemap[i] = index;
            }
        }
        else
        {
            var hashedVerts = new Dictionary<FMeshUVFloat, int>();
            for(int i=0; i < vertCount; i++)
            {
                var PositionA = verts[i].UV;
                bool bFound = hashedVerts.Keys.Contains(PositionA); // (PositionA);
                if ( !bFound )
                {
                    uniqueVerts.Add(i);
                    var index = uniqueVerts.Count - 1;
                    vertRemap[i] = index;
                    hashedVerts.Add(PositionA, index);
                }
                else
                {
                    vertRemap[i] = hashedVerts[PositionA];
                }
            }
        }

        return (vertRemap, uniqueVerts.ToArray());
    }
}
