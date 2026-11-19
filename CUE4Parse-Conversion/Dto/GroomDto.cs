using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Exports.HairStrands;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Versions;
using static CUE4Parse.UE4.Assets.Exports.HairStrands.FHairStrandsBulkData.EDataFlags;

namespace CUE4Parse_Conversion.Dto;

public sealed class GroomDto
{
    public readonly GroomGroupDto[] Groups;

    public GroomDto(UGroomAsset groom, CancellationToken ct)
    {
        var groups = groom.HairGroupsPlatformData;
        var rendering = groom.HairGroupsRendering;
        Groups = new GroomGroupDto[groups.Length];
        for (var i = 0; i < groups.Length; i++)
        {
            ct.ThrowIfCancellationRequested();
            var group = groups[i];

            var renderingGroup = i < rendering.Length ? rendering[i] : null;
            var geometrySettings = renderingGroup?.GeometrySettings ?? FHairGeometrySettings.Default;

            var strands = ConvertCurves(group.Strands.BulkData, groom.Game, geometrySettings, ct);
            var guides = ConvertCurves(group.Guides.BulkData, groom.Game, FHairGeometrySettings.Default, ct);
            Groups[i] = new GroomGroupDto(i, strands, guides);
        }
    }

    public static GroomCurvesDto? ConvertCurves(FHairStrandsBulkData? bulk, EGame? game, FHairGeometrySettings geometrySettings, CancellationToken ct)
    {
        if (bulk?.Header is not { CurveCount: > 0, PointCount: > 0 } header || bulk.Data is not { } data)
            return null;
        // TODO: find samples and add support
        if (header.Flags.HasFlag(DataFlags_HasTranscodedPosition))
            throw new NotSupportedException("Exporting groom using transcoded positions is not supported.");

        ct.ThrowIfCancellationRequested();
        ReadOnlySpan<byte> curveBytes = data.Curves.ReadDataOnce();
        ReadOnlySpan<byte> pointBytes = data.Positions?.ReadDataOnce();
        if (header.Strides is not { CurveStride: 4, PositionStride: 8 } || (curveBytes.Length, pointBytes.Length) != (header.CurveCount * 4L, header.PointCount * 8L))
            throw new InvalidDataException("Invalid groom curve or position bulk data.");

        var packedCurves = MemoryMarshal.Cast<byte, int>(curveBytes);
        var packedPoints = MemoryMarshal.Cast<byte, PackedPoint>(pointBytes);

        var sizes = new int[packedCurves.Length];
        var pointCount = packedPoints.Length;
        var offset = 0;
        for (var i = 0; i < sizes.Length; i++)
        {
            var curve = packedCurves[i];
            var count = curve >>> 24;
            if ((curve & 0xFFFFFF) != offset || count < 2)
                throw new InvalidDataException("Invalid groom curve point range.");

            sizes[i] = count;
            offset += count;
        }

        if (offset != pointCount)
            throw new InvalidDataException("Groom curve ranges do not cover the position buffer.");

        var points = new GroomPointDto[pointCount];
        var center = (header.BoundingBox.Min + header.BoundingBox.Max) * 0.5f;
        var maxRadius = geometrySettings.HairWidth_Override ? geometrySettings.HairWidth * 0.5f : header.MaxRadius;
        var rootScale = geometrySettings.HairRootScale;
        var scaleRange = geometrySettings.HairTipScale - rootScale;

        // UE 5.6 moves the root to tip coordinate to the first byte and the radius to the second
        var newRadiusLayout = game >= EGame.GAME_UE5_6;
        var radiusShift = newRadiusLayout ? 8 : 2;
        var coordShift = newRadiusLayout ? 0 : 8;

        // In batch for cancellation
        const int pointsPerBatch = 16 * 1024;
        for (var first = 0; first < points.Length;)
        {
            ct.ThrowIfCancellationRequested();
            var count = Math.Min(pointsPerBatch, points.Length - first);
            var source = packedPoints.Slice(first, count);
            var destination = points.AsSpan(first, count);
            for (var i = 0; i < source.Length; i++)
            {
                ref readonly var point = ref source[i];
                var position = new FVector((float) point.X, (float) point.Y, (float) point.Z) + center;
                // Normalize the 6 bit radius and 8 bit root to tip coordinate to [0, 1]
                var radiusFraction = ((point.Attributes >> radiusShift) & 0x3F) / 63f;
                var coord = ((point.Attributes >> coordShift) & 0xFF) / 255f;
                var scale = rootScale + scaleRange * coord;
                destination[i] = new GroomPointDto(position, maxRadius * radiusFraction * scale);
            }
            first += count;
        }

        return new GroomCurvesDto(sizes, points, ReadRootUvs(header, data, sizes.Length, ct));
    }

    private static FVector2D[]? ReadRootUvs(FHairStrandsBulkData.FHeader header, FHairStrandsBulkData.FData data, int curveCount, CancellationToken ct)
    {
        const uint rootUvAttribute = 1;
        var uvOffset = header.CurveAttributeOffsets[0];
        if ((header.ImportedAttributes & rootUvAttribute) == 0 || uvOffset == uint.MaxValue)
            return null;

        ReadOnlySpan<byte> bytes = data.CurveAttributes.ReadDataOnce();
        var chunkSize = header.Strides.CurveAttributeChunkStride;
        var curvesPerChunk = header.Strides.CurveAttributeChunkElementCount;
        if (chunkSize == 0 || curvesPerChunk == 0)
            throw new InvalidDataException("Missing groom curve attribute data.");

        var uvs = new FVector2D[curveCount];
        for (int first = 0, chunk = 0; first < uvs.Length; chunk++)
        {
            ct.ThrowIfCancellationRequested();
            var count = (int) Math.Min(curvesPerChunk, uvs.Length - first);
            var offset = chunk * chunkSize + uvOffset;
            if (offset + (long) count * sizeof(uint) > bytes.Length)
                throw new InvalidDataException("Invalid groom root UV range.");

            var source = MemoryMarshal.Cast<byte, uint>(bytes.Slice((int) offset, count * sizeof(uint)));
            var destination = uvs.AsSpan(first, count);
            for (var i = 0; i < source.Length; i++)
            {
                var uv = source[i];
                // Each UV axis has an 11 bit fraction and a 5 bit tile index
                destination[i] = new FVector2D(
                    ((uv >> 22) & 31) + (uv & 2047) / 2047f,
                    ((uv >> 27) & 31) + ((uv >> 11) & 2047) / 2047f);
            }
            first += count;
        }

        return uvs;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct PackedPoint(Half x, Half y, Half z, ushort attributes)
    {
        public readonly Half X = x;
        public readonly Half Y = y;
        public readonly Half Z = z;
        public readonly ushort Attributes = attributes;
    }
}

public sealed class GroomGroupDto(int index, GroomCurvesDto? strands, GroomCurvesDto? guides)
{
    public readonly int Index = index;
    public readonly GroomCurvesDto? Strands = strands;
    public readonly GroomCurvesDto? Guides = guides;
}

public sealed class GroomCurvesDto(int[] sizes, GroomPointDto[] points, FVector2D[]? rootUvs)
{
    public readonly int[] Sizes = sizes;
    public readonly GroomPointDto[] Points = points;
    public readonly FVector2D[]? RootUvs = rootUvs;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct GroomPointDto(FVector position, float radius)
{
    public readonly FVector Position = position;
    public readonly float Radius = radius;
}
