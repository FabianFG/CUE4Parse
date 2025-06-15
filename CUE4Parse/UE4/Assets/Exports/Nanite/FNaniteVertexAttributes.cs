using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.Nanite;

public class FNaniteVertexAttributes
{
    /// <summary>The normals of the vertex.</summary>
    public FVector Normal;
    /// <summary>The tangent component of the vertex + the sign as w (tx,ty,tz,sign).</summary>
    public FVector4 TangentXAndSign;
    /// <summary>The color of the vertex.</summary>
    public FVector4 Color;
    /// <summary>The uv coordinates of the vertex.</summary>
    public FVector2D[] UVs = new FVector2D[NaniteConstants.NANITE_MAX_UVS];
}
