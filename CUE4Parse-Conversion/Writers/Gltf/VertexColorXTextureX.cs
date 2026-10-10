using System.Numerics;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Memory;
using SharpGLTF.Schema2;

namespace CUE4Parse_Conversion.Writers.Gltf;

public struct VertexColorXTextureX : IVertexMaterial, IVertexCustom, IEquatable<VertexColorXTextureX>
{
    // Preserves vertex color masks without affecting the material's color or opacity
    // Custom attributes must start with an underscore!!!
    private const string ShaderMaskAttribute = "_UE_COLOR_0";
    private readonly bool _shaderMask;

    internal uint? SourceIndex { readonly get; set; }

    public readonly int MaxColors => _shaderMask ? 0 : 1;
    public readonly int MaxTextCoords => Constants.MAX_MESH_UV_SETS;

    public Vector4 Color;

    public Vector2 TexCoord0;
    public Vector2 TexCoord1;
    public Vector2 TexCoord2;
    public Vector2 TexCoord3;
    public Vector2 TexCoord4;
    public Vector2 TexCoord5;
    public Vector2 TexCoord6;
    public Vector2 TexCoord7;

    public VertexColorXTextureX(Vector2[] texCoords, Vector4? color = null, bool shaderMask = false)
    {
        _shaderMask = shaderMask;
        Color = color ?? Vector4.One;
        TexCoord0 = texCoords.Length > 0 ? texCoords[0] : Vector2.Zero;
        TexCoord1 = texCoords.Length > 1 ? texCoords[1] : Vector2.Zero;
        TexCoord2 = texCoords.Length > 2 ? texCoords[2] : Vector2.Zero;
        TexCoord3 = texCoords.Length > 3 ? texCoords[3] : Vector2.Zero;
        TexCoord4 = texCoords.Length > 4 ? texCoords[4] : Vector2.Zero;
        TexCoord5 = texCoords.Length > 5 ? texCoords[5] : Vector2.Zero;
        TexCoord6 = texCoords.Length > 6 ? texCoords[6] : Vector2.Zero;
        TexCoord7 = texCoords.Length > 7 ? texCoords[7] : Vector2.Zero;
    }

    void IVertexMaterial.SetColor(int setIndex, Vector4 color)
    {
        Color = color;
    }

    void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord)
    {
        switch (setIndex)
        {
            case 0: TexCoord0 = coord; break;
            case 1: TexCoord1 = coord; break;
            case 2: TexCoord2 = coord; break;
            case 3: TexCoord3 = coord; break;
            case 4: TexCoord4 = coord; break;
            case 5: TexCoord5 = coord; break;
            case 6: TexCoord6 = coord; break;
            case 7: TexCoord7 = coord; break;
        }
    }

    public void Add(in VertexMaterialDelta delta)
    {
        Color += delta.GetColor(0);
        TexCoord0 += delta.GetTexCoord(0);
        TexCoord1 += delta.GetTexCoord(1);
        TexCoord2 += delta.GetTexCoord(2);
        TexCoord3 += delta.GetTexCoord(3);
    }

    public readonly VertexMaterialDelta Subtract(IVertexMaterial baseValue)
    {
        return new VertexMaterialDelta(this).Subtract(new VertexMaterialDelta(baseValue));
    }

    public readonly IEnumerable<KeyValuePair<string, AttributeFormat>> GetEncodingAttributes()
    {
        // glTF COLOR_0 multiplies base color and opacity, shader masks use a custom attribute
        var colorAttribute = _shaderMask ? ShaderMaskAttribute : "COLOR_0";
        yield return new(colorAttribute, new AttributeFormat(DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, true));
        for (var i = 0; i < MaxTextCoords; i++)
            yield return new($"TEXCOORD_{i}", new AttributeFormat(DimensionType.VEC2));
    }

    readonly IEnumerable<string> IVertexCustom.CustomAttributes
    {
        get { if (_shaderMask) yield return ShaderMaskAttribute; }
    }

    readonly bool IVertexCustom.TryGetCustomAttribute(string attributeName, out object value)
    {
        value = null!;
        if (!_shaderMask || attributeName != ShaderMaskAttribute)
            return false;

        value = Color;
        return true;
    }

    void IVertexCustom.SetCustomAttribute(string attributeName, object value)
    {
        if (_shaderMask && attributeName == ShaderMaskAttribute && value is Vector4 color) Color = color;
    }

    readonly void IVertexCustom.Validate()
    {
        if (Color != Vector4.Clamp(Color, Vector4.Zero, Vector4.One))
            throw new ArgumentOutOfRangeException(nameof(Color));
    }

    public readonly Vector2 GetTexCoord(int index)
    {
        return index switch
        {
            0 => TexCoord0,
            1 => TexCoord1,
            2 => TexCoord2,
            3 => TexCoord3,
            4 => TexCoord4,
            5 => TexCoord5,
            6 => TexCoord6,
            7 => TexCoord7,
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };
    }

    public readonly Vector4 GetColor(int index)
        => index != 0 ? throw new ArgumentOutOfRangeException(nameof(index)) : Color;


    public readonly bool Equals(VertexColorXTextureX other)
    {
        return other.SourceIndex == SourceIndex && other._shaderMask == _shaderMask && other.Color == Color &&
            other.TexCoord0 == TexCoord0 &&
            other.TexCoord1 == TexCoord1 &&
            other.TexCoord2 == TexCoord2 &&
            other.TexCoord3 == TexCoord3 &&
            other.TexCoord4 == TexCoord4 &&
            other.TexCoord5 == TexCoord5 &&
            other.TexCoord6 == TexCoord6 &&
            other.TexCoord7 == TexCoord7;
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is VertexColorXTextureX x && Equals(x);
    }

    public override readonly int GetHashCode()
    {
        throw new NotImplementedException();
    }

    public static bool operator ==(VertexColorXTextureX left, VertexColorXTextureX right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(VertexColorXTextureX left, VertexColorXTextureX right)
    {
        return !(left == right);
    }
}
