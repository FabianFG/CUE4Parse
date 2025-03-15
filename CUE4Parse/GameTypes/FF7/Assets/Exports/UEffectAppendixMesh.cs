using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using Newtonsoft.Json;
using static CUE4Parse.Utils.TypeConversionUtils;

namespace CUE4Parse.GameTypes.FF7.Assets.Exports;

public class UEffectAppendixMesh : UObject
{
    public int Version;
    public int FullSize;
    public int SplinePathLength;
    public int TrianglesCount;
    public int VerticesCount;
    public float Offset;
    public float TotalTime;
    public ushort[] SplinePath = [];
    public float[] Times = [];
    public float Scale1;
    public float Scale2;
    public FSkinWeightInfo[] SkinWeightVertexBuffer = [];
    public ushort[] IndexBuffer = [];
    public FVector[] PositionVertexBuffer = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        Version = Ar.Read<int>();
        FullSize = Ar.Read<int>();
        var meshOffset = Ar.Position + 20 + Ar.Read<int>() * 2;
        SplinePathLength = Ar.Read<int>();
        TrianglesCount = Ar.Read<int>();
        VerticesCount = Ar.Read<int>();
        var masked = Ar.Read<ushort>();
        Offset = HalfToFloat((ushort)(masked & 0x3FFF));
        TotalTime = HalfToFloat(Ar.Read<ushort>());

        SplinePath = Ar.ReadArray<ushort>(SplinePathLength);
        Times = Ar.ReadArray(TrianglesCount, () => HalfToFloat(Ar.Read<ushort>()));
        Scale1 = HalfToFloat(Ar.Read<ushort>()); // 1.0f maybe max time or a scale
        Scale2 = HalfToFloat(Ar.Read<ushort>()); // 1.0f maybe max time or a scale
        Ar.Position = meshOffset;

        var bufferLength = Ar.Read<int>();
        SkinWeightVertexBuffer = Ar.ReadArray(VerticesCount, () => new FSkinWeightInfo(Ar, false, true));
        Ar.Position += (bufferLength - VerticesCount) * 12;
        bufferLength = Ar.Read<int>();
        IndexBuffer = Ar.ReadArray<ushort>(TrianglesCount * 3);
        Ar.Position += (bufferLength - TrianglesCount * 3) * 2;
        bufferLength = Ar.Read<int>();
        PositionVertexBuffer = Ar.ReadArray<FVector>(VerticesCount);
        Ar.Position += (bufferLength - VerticesCount) * 12;
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(Version));
        writer.WriteValue(Version);

        writer.WritePropertyName(nameof(FullSize));
        writer.WriteValue(FullSize);

        writer.WritePropertyName(nameof(SplinePathLength));
        writer.WriteValue(SplinePathLength);

        writer.WritePropertyName(nameof(TrianglesCount));
        writer.WriteValue(TrianglesCount);

        writer.WritePropertyName(nameof(VerticesCount));
        writer.WriteValue(VerticesCount);

        writer.WritePropertyName(nameof(Offset));
        writer.WriteValue(Offset);

        writer.WritePropertyName(nameof(TotalTime));
        writer.WriteValue(TotalTime);

        writer.WritePropertyName(nameof(SplinePath));
        serializer.Serialize(writer, SplinePath);

        writer.WritePropertyName(nameof(Times));
        serializer.Serialize(writer, Times);

        writer.WritePropertyName(nameof(Scale1));
        writer.WriteValue(Scale1);

        writer.WritePropertyName(nameof(Scale2));
        writer.WriteValue(Scale2);

        writer.WritePropertyName(nameof(SkinWeightVertexBuffer));
        serializer.Serialize(writer, SkinWeightVertexBuffer);

        writer.WritePropertyName(nameof(IndexBuffer));
        serializer.Serialize(writer, IndexBuffer);

        writer.WritePropertyName(nameof(PositionVertexBuffer));
        serializer.Serialize(writer, PositionVertexBuffer);
    }
}
