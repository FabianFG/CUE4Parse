
using System;
using System.Runtime.InteropServices;
using System.Text;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse_Conversion.Meshes.PSK;

namespace CUE4Parse_Conversion.Meshes.Filmbox
{
    /*
    struct TArray
    {
        T* Data;
        int Count;
        size_t Max;
    }
    */
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct TArray<T>
    {
        public readonly IntPtr Data;
        public readonly int Count;
        public readonly long Max;

        public unsafe TArray(T[] data)
        {
            fixed (T* ptr = data)
            {
                Data = (IntPtr)ptr ;
            }
            Count = data.Length;
            Max = data.LongLength;
        }

        // destructor
        public void Dispose()
        {
            Marshal.FreeHGlobal((IntPtr)Data);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MeshVertex // same as CMeshVertex 
    {
        public FVector Position;
        public FVector Normal;
        public FVector Tangent;
        public FMeshUVFloat UV;

        public MeshVertex(CMeshVertex vert)
        {
            Position = vert.Position;
            Normal = (FVector)vert.Normal;
            Tangent = (FVector)vert.Tangent;
            UV = vert.UV;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MeshSection
    {
        public int MaterialIndex; 
        public int FirstIndex;
        public int NumFaces;
        public TArray<char> MaterialName;

        public MeshSection(CMeshSection section)
        {
            MaterialIndex = section.MaterialIndex;
            FirstIndex = section.FirstIndex;
            NumFaces = section.NumFaces;
            MaterialName = new TArray<char>((section.MaterialName ?? $"material_{MaterialIndex}").ToCharArray());
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class BaseMeshLod
    {
        public int NumVertices { get; set; }
        public int NumTexCoords { get; set; }
        public TArray<MeshSection> Sections { get; set; }
        public TArray<TArray<FMeshUVFloat>> ExtraUVs { get; set; }
        public TArray<FColor> VertexColors { get; set; }
        public TArray<uint> Indices { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class StaticMeshLod : BaseMeshLod
    {
        public TArray<MeshVertex> Vertices { get; set; }
    };
}