using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;

namespace CUE4Parse_Conversion.Meshes.USD;

public sealed class UsdMeshLodBuilder
{
    private readonly string _primName;

    // Core geometry – required
    private IEnumerable<FVector>? _positions;
    private IEnumerable<uint>? _indices;
    private FBox? _bounds;

    // Per-vertex attributes – optional
    private IEnumerable<FVector4>? _normals;
    private IEnumerable<FVector4>? _tangents;
    private IEnumerable<FMeshUVFloat>? _primaryUv;
    private readonly List<(int Index, IEnumerable<FMeshUVFloat> UVs)> _extraUvSets = [];
    private IEnumerable<FColor>? _vertexColors;
    private bool _doubleSided;

    // Skinning
    private int _skinningElementSize;
    private IEnumerable<ushort>? _jointIndices;
    private IEnumerable<float>? _jointWeights;

    // Sections / material subsets
    private IEnumerable<CMeshSection>? _sections;

    private UsdMeshLodBuilder(string primName)
    {
        _primName = primName ?? throw new ArgumentNullException(nameof(primName));
    }

    public UsdMeshLodBuilder WithGeometry(IEnumerable<FVector> positions, IEnumerable<uint> indices)
    {
        _positions = positions;
        _indices = indices;
        return this;
    }

    public UsdMeshLodBuilder WithBounds(FBox bounds) { _bounds = bounds; return this; }
    public UsdMeshLodBuilder WithNormals(IEnumerable<FVector4> normals) { _normals = normals; return this; }
    public UsdMeshLodBuilder WithTangents(IEnumerable<FVector4> tangents) { _tangents = tangents; return this; }
    public UsdMeshLodBuilder WithPrimaryUV(IEnumerable<FMeshUVFloat> uvs) { _primaryUv = uvs; return this; }
    public UsdMeshLodBuilder WithExtraUVSet(int setIndex, IEnumerable<FMeshUVFloat> uvs) { _extraUvSets.Add((setIndex, uvs)); return this; }
    public UsdMeshLodBuilder WithVertexColors(IEnumerable<FColor> colors) { _vertexColors = colors; return this; }
    public UsdMeshLodBuilder WithDoubleSided(bool doubleSided) { _doubleSided = doubleSided; return this; }
    public UsdMeshLodBuilder WithSections(IEnumerable<CMeshSection> sections) { _sections = sections; return this; }

    public UsdMeshLodBuilder WithSkinning(int elementSize, IEnumerable<ushort> jointIndices, IEnumerable<float> jointWeights)
    {
        _skinningElementSize = elementSize;
        _jointIndices = jointIndices;
        _jointWeights = jointWeights;
        return this;
    }

    public static UsdPrim BuildFromLod<TVertex>(string primName, CMeshLod<TVertex> lod, FBox meshBounds) where TVertex : CMeshVertex, new()
    {
        var verts = lod.Verts ?? throw new InvalidOperationException("mesh LOD has no vertices.");
        var indices = lod.Indices?.Value ?? throw new InvalidOperationException("mesh LOD has no indices.");
        var extraUvSets = lod.ExtraUV?.Value ?? [];

        var builder = new UsdMeshLodBuilder(primName)
            .WithGeometry(verts.Select(v => v.Position), indices.Select(i => i))
            .WithNormals(verts.Select(v => v.Normal))
            .WithTangents(verts.Select(v => v.Tangent))
            .WithPrimaryUV(verts.Select(v => v.UV))
            .WithDoubleSided(lod.IsTwoSided);

        if (meshBounds.IsValid != 0) builder.WithBounds(meshBounds);
        if (lod.Sections?.Value is { } sections) builder.WithSections(sections);
        if (lod.VertexColors is { Length: > 0 } colors) builder.WithVertexColors(colors);

        for (var i = 0; i < extraUvSets.Length; i++)
            builder.WithExtraUVSet(i + 1, extraUvSets[i]);

        if (verts is CSkelMeshVertex[] skelVerts)
            ExtractSkinning(builder, skelVerts);

        return builder.Build();
    }

    private static void ExtractSkinning(UsdMeshLodBuilder builder, CSkelMeshVertex[] verts)
    {
        var elementSize = verts.Max(v => v.Influences.Count);
        if (elementSize <= 0) return;

        var jointIndices = new List<ushort>(verts.Length * elementSize);
        var jointWeights = new List<float>(verts.Length * elementSize);

        foreach (var v in verts)
        {
            var influences = v.Influences;
            for (var j = 0; j < elementSize; j++)
            {
                if (j < influences.Count)
                {
                    jointIndices.Add(influences[j].Bone);
                    jointWeights.Add(influences[j].Weight);
                }
                else
                {
                    jointIndices.Add(0);
                    jointWeights.Add(0f);
                }
            }
        }

        builder.WithSkinning(elementSize, jointIndices, jointWeights);
    }

    private UsdPrim Build()
    {
        var positions = (_positions ?? throw new InvalidOperationException("Positions are required.")).ToArray();
        var indices = (_indices ?? throw new InvalidOperationException("Indices are required.")).ToArray();

        var meshPrim = UsdPrim.Def("Mesh", _primName);

        meshPrim.Add(UsdAttribute.Uniform("token", "subdivisionScheme", "none"));
        meshPrim.Add(new UsdAttribute("point3f[]", "points", UsdValue.Array(positions.Select(p => UsdValue.Tuple(p.X, -p.Y, p.Z))))); // MIRROR_MESH
        meshPrim.Add(new UsdAttribute("int[]", "faceVertexCounts", UsdValue.Array(Enumerable.Repeat(3, indices.Length / 3))));
        meshPrim.Add(new UsdAttribute("int[]", "faceVertexIndices", UsdValue.Array(indices.Select(i => (int) i))));

        if (_bounds is { } bounds)
        {
            meshPrim.Add(new UsdAttribute("float3[]", "extent", UsdValue.Array(
                UsdValue.Tuple(bounds.Min.X, bounds.Min.Y, bounds.Min.Z),
                UsdValue.Tuple(bounds.Max.X, bounds.Max.Y, bounds.Max.Z))));
        }

        // Normals: normal3f[]
        if (_normals is not null)
        {
            meshPrim.AddPrimvar("normal3f[]", "normals",
                UsdValue.Array(_normals.Select(n =>
                {
                    var normal = (FVector) n;
                    normal /= MathF.Sqrt(normal | normal);
                    return UsdValue.Tuple(normal.X, -normal.Y, normal.Z); // MIRROR_MESH
                })), "vertex");
        }

        // Tangents: float3[]
        if (_tangents is not null)
        {
            meshPrim.AddPrimvar("float3[]", "primvars:tangents",
                UsdValue.Array(_tangents.Select(t => UsdValue.Tuple(t.X, -t.Y, t.Z))), // MIRROR_MESH
                "vertex");
        }

        // Primary UV
        if (_primaryUv is not null)
        {
            meshPrim.AddPrimvar("texCoord2f[]", "primvars:st", UsdValue.Array(_primaryUv.Select(uv => UsdValue.Tuple(uv.U, uv.V))), "vertex");
        }

        // Extra UV sets (st1, st2, …)
        foreach (var (uvIndex, uvSet) in _extraUvSets)
        {
            meshPrim.AddPrimvar("texCoord2f[]", $"primvars:st{uvIndex}", UsdValue.Array(uvSet.Select(uv => UsdValue.Tuple(uv.U, uv.V))), "vertex");
        }

        // Vertex colours
        if (_vertexColors is not null)
        {
            meshPrim.AddPrimvar("color3f[]", "primvars:displayColor", UsdValue.Array(_vertexColors.Select(c => UsdValue.Tuple(c.R / 255f, c.G / 255f, c.B / 255f))), "vertex");
            meshPrim.AddPrimvar("float[]", "primvars:displayOpacity", UsdValue.Array(_vertexColors.Select(c => UsdValue.Float(c.A / 255f))), "vertex");
        }

        meshPrim.Add(UsdAttribute.Uniform("bool", "doubleSided", _doubleSided));

        // Skinning
        if (_jointIndices is not null && _jointWeights is not null && _skinningElementSize > 0)
        {
            meshPrim.AddMetadata("prepend apiSchemas", UsdValue.Array(UsdValue.Token("SkelBindingAPI")));

            var jointIndicesAttr = UsdAttribute.Primvar("int[]", "primvars:skel:jointIndices", UsdValue.Array(_jointIndices), "vertex");
            jointIndicesAttr.Metadata.Add(new UsdMetadata("elementSize", _skinningElementSize));
            meshPrim.Add(jointIndicesAttr);

            var jointWeightsAttr = UsdAttribute.Primvar("float[]", "primvars:skel:jointWeights", UsdValue.Array(_jointWeights), "vertex");
            jointWeightsAttr.Metadata.Add(new UsdMetadata("elementSize", _skinningElementSize));
            meshPrim.Add(jointWeightsAttr);
        }

        // Material subsets
        if (_sections is not null)
        {
            var sectionArray = _sections.ToArray();
            for (var i = 0; i < sectionArray.Length; i++)
            {
                var subset = BuildSectionSubset(i, sectionArray[i]);
                if (subset is not null) meshPrim.Add(subset);
            }
        }

        return meshPrim;
    }

    private UsdPrim? BuildSectionSubset(int sectionIndex, CMeshSection section)
    {
        if (section.NumFaces <= 0) return null;

        var firstFace = section.FirstIndex / 3;
        var faceIndices = Enumerable.Range(firstFace, section.NumFaces);

        var subsetName = SanitizePrimName(section.MaterialName) ?? $"Section_{sectionIndex}";
        var subset = UsdPrim.Def("GeomSubset", subsetName);

        subset.Add(UsdAttribute.Uniform("token", "elementType", "face"));
        subset.Add(UsdAttribute.Uniform("token", "familyName", "materialBind"));
        subset.Add(new UsdAttribute("int[]", "indices", UsdValue.Array(faceIndices)));

        subset.Add(UsdAttribute.CustomUniform("int", "unrealMaterialIndex", section.MaterialIndex));
        subset.Add(UsdAttribute.CustomUniform("bool", "unrealCastShadow", section.CastShadow));

        if (!string.IsNullOrWhiteSpace(section.MaterialName))
            subset.Add(UsdAttribute.CustomUniform("string", "unrealMaterialName", section.MaterialName));

        var materialPath = section.Material?.GetPathName();
        if (!string.IsNullOrWhiteSpace(materialPath))
            subset.Add(UsdAttribute.CustomUniform("string", "unrealMaterialPath", materialPath));

        return subset;
    }

    public static string? SanitizePrimName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var chars = name.Trim()
            .Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_')
            .ToArray();

        if (chars.Length == 0) return null;
        return char.IsDigit(chars[0]) ? "_" + new string(chars) : new string(chars);
    }
}


