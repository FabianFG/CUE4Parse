using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.Nascar.Assets.Exports;

public class UIRMesh : UObject
{
    public FBox Bounds { get; private set; } = new(FVector.ZeroVector, FVector.OneVector);
    public FBox SecondaryBounds { get; private set; } = new(FVector.ZeroVector, FVector.OneVector);

    public FIRMeshGeneralInfo Info { get; private set; }
    public FIRMeshBuffer[] MeshBuffers { get; private set; } = [];
    public FIRMeshSection[] Sections { get; private set; } = [];
    public FIRMeshSkeleton[] Skeleton { get; private set; } = [];
    public FPackageIndex[] Materials { get; private set; } = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (!Ar.ReadBoolean())
            return;

        Bounds = new FBoxSphereBounds(Ar).GetBox();
        SecondaryBounds = new FBoxSphereBounds(Ar).GetBox();
        Info = Ar.Read<FIRMeshGeneralInfo>();
        Materials = new FPackageIndex[Info.MaterialCount];
        MeshBuffers = Ar.ReadArray(Info.MeshCount, () => new FIRMeshBuffer(Ar));
        Sections = Ar.ReadBulkArray<FIRMeshSection>();
        Skeleton = Ar.ReadBulkArray<FIRMeshSkeleton>();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(Bounds));
        serializer.Serialize(writer, Bounds);

        writer.WritePropertyName(nameof(SecondaryBounds));
        serializer.Serialize(writer, SecondaryBounds);

        writer.WritePropertyName(nameof(Info));
        serializer.Serialize(writer, Info);

        writer.WritePropertyName(nameof(MeshBuffers));
        serializer.Serialize(writer, MeshBuffers);

        writer.WritePropertyName(nameof(Sections));
        serializer.Serialize(writer, Sections);

        writer.WritePropertyName(nameof(Skeleton));
        serializer.Serialize(writer, Skeleton);
    }
}

public sealed class FIRMeshBuffer
{
    public FIRMeshInfo Info { get; }
    public int VertexCount { get; }
    public int PositionStride { get; }
    public int UVChannelCount { get; }
    public int IndexStride { get; }
    public int IndexCount => IndexData.Length / IndexStride;

    [JsonIgnore] public byte[] PositionData { get; }
    [JsonIgnore] public byte[] TangentData { get; }
    [JsonIgnore] public byte[] UVData { get; }
    [JsonIgnore] public byte[] IndexData { get; }

    public FIRMeshBuffer(FAssetArchive Ar)
    {
        Info = new FIRMeshInfo(Ar);
        PositionData = Ar.ReadBulkArray<byte>();

        TangentData = Ar.ReadBulkArray<byte>();
        TensorUtils.Xor(TangentData, (byte) 0x80);

        UVData = Ar.ReadBulkArray<byte>();
        IndexStride = Ar.Read<int>();
        IndexData = Ar.ReadBulkArray<byte>();

        if (TangentData.Length == 0 || TangentData.Length % 8 != 0)
            throw new ParserException(Ar, $"Invalid IRMesh tangent buffer size {TangentData.Length}");

        VertexCount = TangentData.Length / 8;
        if (PositionData.Length % VertexCount != 0 || UVData.Length % (VertexCount * 8) != 0)
            throw new ParserException(Ar, "Invalid IRMesh vertex buffers");

        PositionStride = PositionData.Length / VertexCount;
        UVChannelCount = UVData.Length / (VertexCount * 8);
        if (PositionStride < 12 || IndexStride is not (2 or 4) || IndexData.Length % IndexStride != 0)
            throw new ParserException(Ar, "Invalid IRMesh buffer format");

    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct FIRMeshSection
{
    public readonly byte LOD;
    public readonly byte BufferIndex;
    public readonly ushort MaterialIndex;
    public readonly int FirstIndex;
    public readonly int NumTriangles;
    public readonly int MinVertexIndex;
    public readonly int MaxVertexIndex;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly struct FIRMeshSkeleton
{
    public readonly int Index;
    public readonly FVector4 Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct FIRMeshGeneralInfo
{
    public readonly int Unknown1;
    public readonly int Unknown2;
    public readonly int MaterialCount;
    public readonly int Unknown3;
    public readonly int MeshCount;
}

public readonly struct FIRMeshInfo(FAssetArchive Ar)
{
    public readonly int NumCoords = Ar.Read<int>();
    public readonly bool UVEncoding = Ar.ReadBoolean();
    public readonly bool IsSkeletalMesh = Ar.ReadBoolean();
    public readonly bool Unknown1 = Ar.ReadBoolean();
    public readonly float Unknown2 = Ar.Read<float>();
}
