using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

/// <summary>
/// A set of triangles which are rendered with the same material.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct FGeometryCollectionSection
{
    /// <summary>
    /// The index of the material with which to render this section.
    /// </summary>
    public readonly int MaterialID;

    /// <summary>
    /// Range of vertices and indices used when rendering this section.
    /// </summary>
    public readonly int FirstIndex;
    public readonly int NumTriangles;
    public readonly int MinVertexIndex;
    public readonly int MaxVertexIndex;
}
