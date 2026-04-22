using System;
using System.Collections.Generic;
using CUE4Parse_Conversion.Materials;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;

namespace CUE4Parse_Conversion.Meshes.PSK;

[Obsolete("This class is deprecated and will be removed in a future release. Please use CMeshLod<TVertex> instead.")]
public abstract class CMeshLod : CMeshLod<CMeshVertex>;
public abstract class CMeshLod<TVertex> : IDisposable where TVertex : CMeshVertex, new()
{
    public int NumVerts = 0;
    public int NumTexCoords = 0;
    public float ScreenSize = 0f;
    public bool HasNormals = false;
    public bool HasTangents = false;
    public bool IsTwoSided = false;
    public bool IsNanite = false;
    public Lazy<CMeshSection[]>? Sections;
    public Lazy<FMeshUVFloat[][]>? ExtraUV;
    public FColor[]? VertexColors;
    public CVertexColor[]? ExtraVertexColors;
    public Lazy<uint[]>? Indices;
    public TVertex[]? Verts;
    public bool SkipLod => Sections?.Value.Length < 1 || Indices?.Value.Length < 1;

    public void AllocateVerts(int count)
    {
        Verts = new TVertex[count];
        for (var i = 0; i < Verts.Length; i++)
        {
            Verts[i] = new TVertex();
        }

        NumVerts = count;
        AllocateUVBuffers();
    }

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

    public void BuildNormals()
    {
        if (HasNormals) return;
        // BuildNormalsCommon(Verts, Indices);
        HasNormals = true;
    }

    public void BuildTangents()
    {
        if (HasTangents) return;
        // BuildTangentsCommon(Verts, Indices);
        HasTangents = true;
    }

    public virtual void Dispose()
    {
        if (Sections is not null && Sections.IsValueCreated)
        {
            Array.Clear(Sections.Value);
            Sections = null;
        }

        if (ExtraUV is not null && ExtraUV.IsValueCreated)
        {
            foreach (var uv in ExtraUV.Value)
            {
                Array.Clear(uv);
            }
            Array.Clear(ExtraUV.Value);
            ExtraUV = null;
        }

        if (VertexColors is not null)
        {
            Array.Clear(VertexColors);
            VertexColors = null;
        }

        if (ExtraVertexColors is not null)
        {
            foreach (var vc in ExtraVertexColors)
            {
                vc.Dispose();
            }
            Array.Clear(ExtraVertexColors);
            ExtraVertexColors = null;
        }

        if (Indices is not null && Indices.IsValueCreated)
        {
            Array.Clear(Indices.Value);
            Indices = null;
        }

        if (Verts is not null)
        {
            Array.Clear(Verts);
            Verts = null;
        }
    }
}

public struct CVertexColor(string name, FColor[]? colorData) : IDisposable
{
    public readonly string Name = name;
    public FColor[]? ColorData = colorData;

    public void Dispose()
    {
        if (ColorData is not null)
        {
            Array.Clear(ColorData);
            ColorData = null;
        }
    }
}
