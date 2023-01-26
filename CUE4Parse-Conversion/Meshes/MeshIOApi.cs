using System;
using System.Collections.Generic;
using System.Linq;
using CSMath;
using CUE4Parse_Conversion.Materials;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Writers;
using MeshIO;
using MeshIO.Elements;
using MeshIO.Elements.Geometries.Layers;
using MeshIO.FBX;

namespace CUE4Parse_Conversion.Meshes;

public class MeshIOApi
{
    public readonly Scene MeshIOScene;

    public MeshIOApi(string name, CStaticMeshLod lod, List<MaterialExporter2>? materialExports, ExporterOptions options)
    {
        MeshIOScene = new Scene(name);
        var meshNode = new Node(name);
        var mesh = new MeshIO.Elements.Geometries.Mesh(name);

        ExportStaticMesh(lod, lod.Sections.Value, materialExports, mesh, options);

        meshNode.Children.Add(mesh);
        MeshIOScene.Nodes.Add(meshNode);
    }

    public void Save(EMeshFormat meshFormat, FArchiveWriter Ar)
    {
        if (meshFormat == EMeshFormat.FBX)
        {
            new FbxWriter(Ar.GetStream()).WriteAscii(MeshIOScene);
            return;
        }
        throw new ArgumentOutOfRangeException();
    }

    public void ExportStaticMesh(CStaticMeshLod lod, CMeshSection[] sects, List<MaterialExporter2>? materialExports, MeshIO.Elements.Geometries.Mesh mesh, ExporterOptions options)
    {
        (int[] vertRemap, int[] uniqueVerts) = DetermineVertsToWeld(lod);

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
        mesh.Vertices = meshVert.ToList();

        var materialLayers = new List<LayerElementMaterial>();
        var Indices = new List<int>();
        for (int sectIndex = 0; sectIndex < numSections; sectIndex++)
        {
            var sect = sects[sectIndex];

            var matLayer = new LayerElementMaterial()
            {
                Name = sect.MaterialName,
                MappingMode = MappingMode.ByPolygon, ReferenceMode = ReferenceMode.IndexToDirect
            };

            for (int faceIndex = 0; faceIndex < sect.NumFaces; faceIndex++)
            {
                var wedgeIndex = new int[3];
                for (var pointIndex = 0; pointIndex < 3; pointIndex++)
                {
                    var index = lod.Indices.Value[sect.FirstIndex + ((faceIndex * 3) + pointIndex)];
                    var reMappedIndex = vertRemap[index];
                    wedgeIndex[pointIndex] = reMappedIndex;

                    Indices.Add(index);
                    matLayer.Indices.Add(index);
                }
                mesh.Polygons.Add(new Triangle((uint)wedgeIndex[0], (uint)wedgeIndex[1], (uint)wedgeIndex[2]));
            }
            materialLayers.Add(matLayer);
        }

        // normal layer needs to be first?
        var normalLayer = new LayerElementNormal() { MappingMode = MappingMode.ByPolygonVertex, ReferenceMode = ReferenceMode.Direct };
        var tangentLayer = new LayerElementTangent() { MappingMode = MappingMode.ByPolygonVertex, ReferenceMode = ReferenceMode.Direct };
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

        for (int i = 0; i < Indices.Count; i++)
        {
            var index = Indices[i];
            normalLayer.Normals.Add(convertedNormals[index]);
            tangentLayer.Tangents.Add(convertedTangents[index]);
        }

        mesh.Layers.Add(normalLayer);
        mesh.Layers.Add(tangentLayer);
        // mesh.Layers.Add(binormalLayer);
        materialLayers.ForEach(x => mesh.Layers.Add(x)); // doesn't work

        // TODO: UVs

    }

    private XYZ CastToXYZ(FVector vec) => new XYZ(vec.X, vec.Z, vec.Y);
    private XYZ CastToXYZNormalize(FVector vec)
    {
        Gltf.SwapYZAndNormalize(vec);
        return new XYZ(vec.X, vec.Y, vec.Z);
    }

    private (int[] VertRemap, int[] UniqueVerts) DetermineVertsToWeld(CStaticMeshLod lod)
    {
        var vertCount = lod.Verts.Length;
        var vertRemap = new int[vertCount];
        var uniqueVerts = new List<int>();

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

        return (vertRemap, uniqueVerts.ToArray());
    }
}
