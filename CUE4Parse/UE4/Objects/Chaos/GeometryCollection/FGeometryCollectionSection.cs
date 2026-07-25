using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

/// <summary>
/// A set of triangles which are rendered with the same material.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct FGeometryCollectionSection
{
    /// <summary>
    /// The index of the material with which to render this section.
    /// </summary>
    public int MaterialID;
    
    /// <summary>
    /// Range of vertices and indices used when rendering this section.
    /// </summary>
    public int FirstIndex;
    public int NumTriangles;
    public int MinVertexIndex;
    public int MaxVertexIndex;
}