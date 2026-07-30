using System.Runtime.InteropServices;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FGeometryCollectionMeshElement
{
    public readonly short TransformIndex;
    public readonly byte MaterialIndex;
    public readonly bool bIsInternal;
    public readonly uint TriangleStart;
    public readonly uint TriangleCount;
    public readonly uint VertexStart;
    public readonly uint VertexEnd;

    public FGeometryCollectionMeshElement(FArchive Ar)
    {
        TransformIndex = Ar.Read<short>();
        MaterialIndex = Ar.Read<byte>();
        bIsInternal = Ar.ReadFlag();
        TriangleStart = Ar.Read<uint>();
        TriangleCount = Ar.Read<uint>();
        VertexStart = Ar.Read<uint>();
        VertexEnd = Ar.Read<uint>();

        if (Ar.Game is GAME_MarvelRivals)
        {
            _ = Ar.ReadArray(2, () => (Ar.Read<FVector>(), Ar.Read<int>()));
        }
    }
};
