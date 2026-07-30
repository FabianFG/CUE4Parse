using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

public readonly struct FGeometryCollectionMeshDescription
{
    public readonly uint NumVertices;
    public readonly uint NumTriangles;
    public readonly FBoxSphereBounds? PreSkinnedBounds;
    public readonly FGeometryCollectionMeshElement[] Sections;
    public readonly FGeometryCollectionMeshElement[] SectionsNoInternal;
    public readonly FGeometryCollectionMeshElement[] SubSections;

    public FGeometryCollectionMeshDescription(FArchive Ar)
    {
        NumVertices = Ar.Read<uint>();
        NumTriangles = Ar.Read<uint>();
        if (Ar.Game < GAME_UE5_7) PreSkinnedBounds = new FBoxSphereBounds(Ar);
        Sections = Ar.ReadArray(() => new FGeometryCollectionMeshElement(Ar));
        if (Ar.Game is GAME_MarvelRivals)
        {
            // sometimes these are different from the normal sections, sometimes they are the same.
            _ = Ar.ReadArray(() => new FGeometryCollectionMeshElement(Ar));
        }
        SectionsNoInternal = Ar.ReadArray(() => new FGeometryCollectionMeshElement(Ar));
        SubSections = Ar.ReadArray(() => new FGeometryCollectionMeshElement(Ar));
        if (Ar.Game is GAME_MarvelRivals)
        {
            
            _ = Ar.ReadArray(() => new FGeometryCollectionMeshElement(Ar));
        }
    }
}
