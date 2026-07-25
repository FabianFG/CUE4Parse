using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

public class FGeometryCollectionMeshDescription
{
    public uint NumVertices;
    public uint NumTriangles;
    public FGeometryCollectionMeshElement[] Sections;
    public FGeometryCollectionMeshElement[] SectionsNoInternal;
    public FGeometryCollectionMeshElement[] SubSections;
    
    public FGeometryCollectionMeshDescription(FArchive Ar)
    {
        NumVertices = Ar.Read<uint>();
        NumTriangles = Ar.Read<uint>();
        
        Sections = Ar.ReadArray<FGeometryCollectionMeshElement>();
        SectionsNoInternal = Ar.ReadArray<FGeometryCollectionMeshElement>();
        SubSections = Ar.ReadArray<FGeometryCollectionMeshElement>();
    }
}