using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.RenderCore;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.Nascar.Assets.Exports;

public class UIRMesh : UObject
{
    public FBoxSphereBounds? Bounds { get; private set; }
    public FBoxSphereBounds? SecondaryBounds { get; private set; }
    public FIRMeshGeneralInfo Info { get; private set; }
    public FIRMeshBuffer[] PartsBuffers { get; private set; } = [];
    public FIRMeshSection[] Sections { get; private set; } = [];
    public FIRMeshSkeleton[] Skeleton { get; private set; } = [];
    public FPackageIndex[] Materials { get; private set; } = [];

    public void SetMaterials(FPackageIndex[] materials)
    {
        Materials = materials;
    }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (!Ar.ReadBoolean())
            return;

        Bounds = new FBoxSphereBounds(Ar);
        SecondaryBounds = new FBoxSphereBounds(Ar);
        Info = Ar.Read<FIRMeshGeneralInfo>();
        Materials = new FPackageIndex[Info.MaterialCount];
        PartsBuffers = Ar.ReadArray(Info.PartCount, () => new FIRMeshBuffer(Ar));
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

        writer.WritePropertyName(nameof(PartsBuffers));
        serializer.Serialize(writer, PartsBuffers);

        writer.WritePropertyName(nameof(Sections));
        serializer.Serialize(writer, Sections);

        writer.WritePropertyName(nameof(Skeleton));
        serializer.Serialize(writer, Skeleton);
    }
}

public sealed class FIRMeshBuffer
{
    public FIRMeshInfo Info { get; }
    public int IndexStride { get; }
    public int PositionStride => Info.MorphsEncoding ? 44 : Info.IsSkeletalMesh ? 20 : 12;

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
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct FIRMeshSection
{
    public readonly byte LOD;
    public readonly byte PartIndex;
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
    public readonly int BonesCount;
    public readonly int MorphRegionCount;
    public readonly int MaterialCount;
    public readonly int LODsCount;
    public readonly int PartCount;
}

public readonly struct FIRMeshInfo(FAssetArchive Ar)
{
    public readonly int NumCoords = Ar.Read<int>();
    public readonly bool ScaledUV = Ar.ReadBoolean();
    public readonly bool IsSkeletalMesh = Ar.ReadBoolean();
    public readonly bool MorphsEncoding = Ar.ReadBoolean();
    public readonly float MorphsScale = Ar.Read<float>();
}

public struct FIRMeshNormals
{
    public FPackedNormal X;
    public FPackedNormal Z;
}

// for the SkeletalMesh case
// public struct FIRMeshPosVec2Int
// {
//     public FVector Position;
//     public InlineArray4<byte> BoneIndex;
//     public InlineArray4<byte> BoneWeight;
// }
