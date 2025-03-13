using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.GameTypes.FF7.Assets.Objects;

public static class FF7FStaticMeshLODResources
{
    public static bool SerializeIndexBuffer(FArchive Ar, out int[] sectionTrianglesCount, out uint[] indexBuffer)
    {
        indexBuffer = [];
        sectionTrianglesCount = [];
        var version = Ar.Read<int>();
        var tempAr = new FByteArchive("FF7Rebirth_SM", Ar.ReadArray<byte>(), Ar.Versions);
        tempAr.Position += 28; // skip header, idk what that data is
        var batches = tempAr.ReadArray<FF7Batch>();
        if (batches.Length == 0) return false;

        var indicesBuffer = tempAr.ReadArray<uint>();
        var batchesBuffer = tempAr.ReadArray<uint>();
        var batchInfos = tempAr.ReadArray(() => new FF7BatchInfo(tempAr));

        var lods = tempAr.ReadArray(() => new FF7Lod(tempAr));
        var batchesIndices = tempAr.ReadArray<int>(); // batches reordering ???
        var somestructs = tempAr.ReadArray(() => new Ff7SomeStruct(tempAr));

        var index = tempAr.Read<int>(); // always 0
        var sectionsIndices = tempAr.ReadArray<uint>();
        var lodinfos = tempAr.ReadArray(() => new FF7LodInfo(tempAr));

        var ib = new List<uint>(batchesBuffer.Length * 3);
        foreach (var batch in batches)
        {
            var vertices = new HashSet<uint>();
            var startIndex = batch.TotalVertices;
            var triangleStartIndex = batch.TotatTriangles;
            for (var i = 0; i < batch.TrianglesCount; i++)
            {
                uint x = batchesBuffer[triangleStartIndex+i];
                var iv = indicesBuffer[startIndex + (x & 0x3ff)];
                var jv = indicesBuffer[startIndex + ((x >> 10) & 0x3ff)];
                var kv = indicesBuffer[startIndex + ((x >> 20) & 0x3ff)];
                ib.Add(iv);
                ib.Add(jv);
                ib.Add(kv);
                vertices.Add(iv);
                vertices.Add(jv);
                vertices.Add(kv);
            }
        }
        var fullIndexBuffer = ib.ToArray();

        List<uint> indexbuffer = [];
        sectionTrianglesCount = new int[sectionsIndices.Length];
        for (int i = 0; i < sectionsIndices.Length; i++)
        {
            var section = lodinfos[sectionsIndices[i]];
            sectionTrianglesCount[i] = section.BatchesCount;
            indexbuffer.AddRange(fullIndexBuffer.AsSpan()[(section.BatchesOffset * 3)..((section.BatchesOffset + section.BatchesCount) * 3)]);
        }

        var sections1 = tempAr.ReadArray<uint>();
        var lodInfos1 = tempAr.ReadArray(() => new FF7LodInfo(tempAr));
        var anotherBuffer = tempAr.ReadArray<uint>();

        tempAr.Dispose();
        indexBuffer = indexbuffer.ToArray();
        return true;
    }
}

public struct FF7Batch
{
    public int VerticesCount;
    public int TotalVertices;
    public int TrianglesCount;
    public int TotatTriangles;
};

public struct FF7BatchInfo(FArchive Ar)
{
    public FVector4 Position1 = Ar.Read<FVector4>();
    public FVector4 Position2 = Ar.Read<FVector4>();
    public FVector SomeVector1= Ar.Read<FHalfVector>();
    public FVector SomeVector2= Ar.Read<FHalfVector>();
    public float Distance = Ar.Read<float>();
    public FVector MinVertexPosition = Ar.Read<FVector>();
    public float SomeFloat = Ar.Read<float>();
    public FVector MaxVertexPosition = Ar.Read<FVector>();
    public uint Hash = Ar.Read<uint>();
}

public struct Ff7SomeStruct(FArchive Ar)
{
    public FVector4[][] Vectors = Ar.ReadArray(3, () => Ar.ReadArray<FVector4>(8));
    public int[] Flags = Ar.ReadArray<int>(8);
}

public struct FF7Lod(FArchive Ar)
{
    public int Offset = Ar.Read<int>();
    public int Count = Ar.Read<int>();
    public int NextOffset = Ar.Read<int>();
    public int NextCount = Ar.Read<int>();

    public FVector4 Position1 = Ar.Read<FVector4>();
    public FVector4 Position2 = Ar.Read<FVector4>();
    public FVector4 Position3 = Ar.Read<FVector4>();
}

public struct FF7LodInfo(FArchive Ar)
{
    public int Index = Ar.Read<int>();
    public int idk = Ar.Read<int>();
    public int Offset = Ar.Read<int>();
    public int Count = Ar.Read<int>();
    public int IndicesOffset = Ar.Read<int>();
    public int IndicesCount = Ar.Read<int>();
    public int BatchesOffset = Ar.Read<int>();
    public int BatchesCount = Ar.Read<int>();
    public int VerticesOffset = Ar.Read<int>();
    public int VerticesCount = Ar.Read<int>();
    public ushort[] SomeInts1 = Ar.ReadArray<ushort>(4);
    public float[] SomeFloats = Ar.ReadArray<float>(5);
    public ushort[] SomeInts2 = Ar.ReadArray<ushort>(14);
}
