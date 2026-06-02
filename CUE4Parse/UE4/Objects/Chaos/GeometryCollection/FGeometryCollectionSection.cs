namespace CUE4Parse.UE4.Assets.Exports.Chaos.GeometryCollection;

public struct FGeometryCollectionSection: IUStruct
{
    /** The index of the material with which to render this section. */
    public int MaterialID;

    /** Range of vertices and indices used when rendering this section. */
    public int FirstIndex;
    public int NumTriangles;
    public int MinVertexIndex;
    public int MaxVertexIndex;
}