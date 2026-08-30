using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.GameTypes.Tencent.GangstarMirageCity.Objects.Meshes;

public sealed class FGangstarPositionVertexBuffer : FPositionVertexBuffer
{
    private readonly FVector3UnsignedShortScale[]? _packedPositions;
    private readonly FIntVector _componentBits;
    private readonly FIntVector _componentMin;

    public FGangstarPositionVertexBuffer(FArchive Ar)
    {
        Stride = Ar.Read<int>();
        NumVertices = Ar.Read<int>();

        if (Ar.Peek<int>() == Stride)
        {
            Verts = Ar.ReadBulkArray<FVector>();
            if (Verts.Length != NumVertices)
                throw new ParserException(Ar, $"NumVertices={Verts.Length} != NumVertices={NumVertices}");
            return;
        }

        var isCompressed = Ar.Read<byte>() != 0 | Ar.Read<byte>() != 0;
        _ = Ar.Read<FVector>(); // Position compression offset
        _componentBits = Ar.Read<FIntVector>();
        _componentMin = Ar.Read<FIntVector>();

        if (!isCompressed)
        {
            Verts = Ar.ReadBulkArray<FVector>();
            if (Verts.Length != NumVertices)
                throw new ParserException(Ar, $"NumVertices={Verts.Length} != NumVertices={NumVertices}");
            return;
        }

        _packedPositions = Ar.ReadBulkArray<FVector3UnsignedShortScale>();
        if (_packedPositions.Length != NumVertices)
            throw new ParserException(Ar, $"NumVertices={_packedPositions.Length} != NumVertices={NumVertices}");
        Verts = new FVector[NumVertices];
    }

    public void Decode(FBoxSphereBounds bounds)
    {
        if (_packedPositions == null) return;

        const int laneRange = 65536;
        var scaleX = MathF.Pow(2.0f, -_componentBits.X);
        var scaleY = MathF.Pow(2.0f, -_componentBits.Y);
        var scaleZ = MathF.Pow(2.0f, -_componentBits.Z);

        var radixX = GetPageCount(bounds.Origin.X + bounds.BoxExtent.X, scaleX, _componentMin.X, laneRange);
        var radixY = GetPageCount(bounds.Origin.Y + bounds.BoxExtent.Y, scaleY, _componentMin.Y, laneRange);

        for (var i = 0; i < Verts.Length; i++)
        {
            var packed = _packedPositions[i];
            var pageIndex = (int) packed.W;
            var pageX = pageIndex % radixX;
            pageIndex /= radixX;
            var pageY = pageIndex % radixY;
            var pageZ = pageIndex / radixY;

            Verts[i] = new FVector(
                (packed.X + pageX * laneRange + _componentMin.X) * scaleX,
                (packed.Y + pageY * laneRange + _componentMin.Y) * scaleY,
                (packed.Z + pageZ * laneRange + _componentMin.Z) * scaleZ);
        }
    }

    private static int GetPageCount(float maximum, float scale, int minimum, int laneRange)
    {
        var quantizedMaximum = (long) MathF.Ceiling(maximum / scale);
        var requiredPages = Math.Max(1, (int) ((quantizedMaximum - minimum + laneRange) / laneRange));
        var pageCount = 1;
        while (pageCount < requiredPages) pageCount <<= 1;
        return pageCount;
    }

}
