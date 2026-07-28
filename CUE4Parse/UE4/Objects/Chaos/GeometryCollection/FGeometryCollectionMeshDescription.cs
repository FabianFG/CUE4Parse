using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

public readonly struct FGeometryCollectionMeshDescription
{
    public readonly uint NumVertices;
    public readonly uint NumTriangles;
    public readonly FGeometryCollectionMeshElement[] Sections;
    public readonly FGeometryCollectionMeshElement[] SectionsNoInternal;
    public readonly FGeometryCollectionMeshElement[] SubSections;

    public FGeometryCollectionMeshDescription(FArchive Ar)
    {
        NumVertices = Ar.Read<uint>();
        NumTriangles = Ar.Read<uint>();

        Sections = Ar.ReadArray<FGeometryCollectionMeshElement>();
        SectionsNoInternal = Ar.ReadArray<FGeometryCollectionMeshElement>();
        SubSections = Ar.ReadArray<FGeometryCollectionMeshElement>();
    }
}
