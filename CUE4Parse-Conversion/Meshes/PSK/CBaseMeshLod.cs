using System;
using System.Collections.Generic;
using CUE4Parse_Conversion.Materials;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;

namespace CUE4Parse_Conversion.Meshes.PSK;

public class CBaseMeshLod
{
    public int NumVerts = 0;
    public int NumTexCoords = 0;
    public bool HasNormals = false;
    public bool HasTangents = false;
    public bool IsTwoSided = false;
    public Lazy<CMeshSection[]> Sections;
    public Lazy<FMeshUVFloat[][]> ExtraUV;
    public FColor[]? VertexColors;
    public CVertexColor[]? ExtraVertexColors;
    public Lazy<FRawStaticIndexBuffer> Indices;
    public bool SkipLod => Sections.Value.Length < 1 || Indices.Value.Length < 1;

    public void AllocateUVBuffers()
    {
        ExtraUV = new Lazy<FMeshUVFloat[][]>(() =>
        {
            var ret = new FMeshUVFloat[NumTexCoords - 1][];
            for (var i = 0; i < ret.Length; i++)
            {
                ret[i] = new FMeshUVFloat[NumVerts];
                for (var j = 0; j < ret[i].Length; j++)
                {
                    ret[i][j] = new FMeshUVFloat(0, 0);
                }
            }
            return ret;
        });
    }

    public void AllocateVertexColorBuffer()
    {
        VertexColors = new FColor[NumVerts];
        for (var i = 0; i < VertexColors.Length; i++)
        {
            VertexColors[i] = new FColor();
        }

        ExtraVertexColors = Array.Empty<CVertexColor>();
    }

    public List<MaterialExporter2> GetMaterials(ExporterOptions options)
    {
        if (SkipLod || !options.ExportMaterials) return [];

        var materials = new List<MaterialExporter2>();
        foreach (var section in Sections.Value)
        {
            if (section.Material?.Load<UMaterialInterface>() is { } material)
            {
                materials.Add(new MaterialExporter2(material, options));
            }
        }
        return materials;
    }
}

public readonly struct CVertexColor
{
    public readonly string Name;
    public readonly FColor[] ColorData;

    public CVertexColor(string name, FColor[]? colorData)
    {
        Name = name;
        ColorData = colorData ?? Array.Empty<FColor>();
    }
}
