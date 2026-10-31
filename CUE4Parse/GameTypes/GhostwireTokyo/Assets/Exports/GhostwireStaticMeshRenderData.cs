using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.RenderCore;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh;

// Snowfall (UE4.26) cooked static mesh render data. Per LOD: section table, 14-byte trailer
// (leading float is the LOD screen size), (12, N)(12, N) position marker, positions, packed
// tangents, vertex-major UVs, then render and depth-only index chunks.
public class GhostwireStaticMeshRenderData : FStaticMeshRenderData
{
    private readonly record struct Block(int Marker, int VertCount);
    private readonly record struct SectionRecord(
        int MaterialIndex, int FirstIndex, int NumTriangles, int MinVertexIndex, int MaxVertexIndex,
        bool EnableCollision, bool CastShadow, bool ForceOpaque, bool VisibleInRayTracing);

    private readonly int _materialSlots;

    public GhostwireStaticMeshRenderData(FAssetArchive Ar, long validPos, int materialSlots)
    {
        _materialSlots = materialSlots;
        var raw = Ar.ReadBytes((int)(validPos - Ar.Position)).ToArray();
        var blocks = ValidateBlocks(raw, FindBlocks(raw));
        if (blocks.Count == 0)
            throw new NotSupportedException("Ghostwire static mesh: no LOD blocks found");

        var lods = new List<FStaticMeshLODResources>();
        for (var i = 0; i < blocks.Count; i++)
            lods.Add(BuildLod(raw, blocks, i));

        LODs = [.. lods];
        Bounds = ComputeBounds([.. lods]);
        bLODsShareStaticLighting = false;
        ScreenSize = new float[MAX_STATIC_LODS_UE4];
        for (var i = 0; i < lods.Count && i < ScreenSize.Length; i++)
            ScreenSize[i] = TryReadScreenSize(raw, blocks[i].Marker);
    }

    private static float TryReadScreenSize(byte[] raw, int marker)
    {
        var o = marker - 14;
        if (o < 0 || o + 4 > raw.Length)
            return 0f;
        return BitConverter.ToSingle(raw, o);
    }

    private static List<Block> FindBlocks(byte[] raw)
    {
        var blocks = new List<Block>();
        // Some compact Ghostwire LOD blocks are aligned to two bytes.
        for (var o = 0; o + 16 <= raw.Length; o += 2)
        {
            if (GetU32(raw, o) != 12 || GetU32(raw, o + 8) != 12)
                continue;
            var n = GetU32(raw, o + 4);
            if (n != GetU32(raw, o + 12) || n < 1 || n > 1_000_000)
                continue;
            var posStart = o + 16;
            if (posStart + n * 12L > raw.Length)
                continue;

            // First position must be a finite, sane-magnitude float to reject coincidental markers.
            var fx = BitConverter.ToSingle(raw, posStart);
            if (!float.IsFinite(fx) || Math.Abs(fx) > 1e6f)
                continue;

            blocks.Add(new Block(o, (int)n));
        }
        blocks.Sort((a, b) => a.Marker.CompareTo(b.Marker));
        return blocks;
    }

    /// <summary>
    /// A coincidental (12,N)(12,N) marker inside another stream (UVs, colors, indices) creates a
    /// phantom block that both fails itself and truncates the REAL block before it (index chunks
    /// behind the phantom fall out of its region). A drawable LOD always carries an index buffer,
    /// so a block whose region yields no index chunk is a phantom: drop it, which re-extends the
    /// previous block's region to the next survivor. Process left-to-right with backtracking so
    /// each survivor is re-validated against whatever block actually follows it.
    /// </summary>
    private static List<Block> ValidateBlocks(byte[] raw, List<Block> candidates)
    {
        if (candidates.Count == 0) return candidates;

        var accepted = new List<Block>();
        foreach (var candidate in candidates)
        {
            while (accepted.Count > 0 && !HasIndexChunk(raw, accepted[^1], candidate.Marker))
                accepted.RemoveAt(accepted.Count - 1);
            accepted.Add(candidate);
        }
        while (accepted.Count > 0 && !HasIndexChunk(raw, accepted[^1], raw.Length))
            accepted.RemoveAt(accepted.Count - 1);
        return accepted;
    }

    /// <summary>Chunk-gate scan (same rules as BuildLod) answering only "is there at least one
    /// index buffer in this block's region". Tries both index widths: a small LOD can still be
    /// authored with 32-bit indices.</summary>
    private static bool HasIndexChunk(byte[] raw, Block b, int regionEnd)
    {
        var scanStart = b.Marker + 16 + (int)b.VertCount * 12;
        foreach (var indexSize in ChunkIndexSizes(b.VertCount))
        {
            for (var x = scanStart; x + 8 <= regionEnd; x += 4)
            {
                var flag = GetU32(raw, x);
                if (flag < 1 || flag > 16)
                    continue;
                var count = (int)GetU32(raw, x + 4);
                if (count < 6 || count > 64_000_000 || count % (indexSize * 3) != 0)
                    continue;
                var dataStart = x + 8;
                if (dataStart + count > regionEnd)
                    continue;

                var indices = count / indexSize;
                var valid = true;
                for (var j = 0; j < indices; j++)
                {
                    var v = indexSize == 2 ? GetU16(raw, dataStart + j * 2) : GetU32(raw, dataStart + j * 4);
                    if (v >= b.VertCount)
                    {
                        valid = false;
                        break;
                    }
                }
                if (valid)
                    return true;
            }
        }
        return false;
    }

    /// <summary>Index width candidates for a block: the stock rule (32 bit once the vertex count
    /// no longer fits 16 bit), then the other width as a fallback.</summary>
    private static int[] ChunkIndexSizes(int vertCount) => vertCount > ushort.MaxValue ? [4, 2] : [2, 4];

    private FStaticMeshLODResources BuildLod(byte[] raw, List<Block> blocks, int index)
    {
        var n = blocks[index].VertCount;
        var posStart = blocks[index].Marker + 16;

        var verts = new FVector[n];
        for (var i = 0; i < n; i++)
        {
            var p = posStart + i * 12;
            verts[i] = new FVector(
                BitConverter.ToSingle(raw, p),
                BitConverter.ToSingle(raw, p + 4),
                BitConverter.ToSingle(raw, p + 8));
        }

        var scanStart = posStart + n * 12;
        var regionEnd = index + 1 < blocks.Count ? blocks[index + 1].Marker : raw.Length;

        // Index chunks: (u32 flag)(u32 byteCount)(indices). Indices widen to 32 bit once the vertex
        // count no longer fits 16 bit, as in stock Unreal. A small LOD can still be authored with
        // 32-bit indices, so when the stock width finds nothing retry with the other one.
        var chunks = new List<uint[]>();
        var chunkOffsets = new List<int>();
        foreach (var indexSize in ChunkIndexSizes(n))
        {
            for (var x = scanStart; x + 8 <= regionEnd; x += 4)
            {
                var flag = GetU32(raw, x);
                if (flag < 1 || flag > 16)
                    continue;
                var count = (int)GetU32(raw, x + 4);
                if (count < 6 || count > 64_000_000 || count % (indexSize * 3) != 0)
                    continue;
                var dataStart = x + 8;
                var dataEnd = dataStart + count;
                if (dataEnd > regionEnd)
                    continue;

                var indices = new uint[count / indexSize];
                var valid = true;
                for (var j = 0; j < indices.Length; j++)
                {
                    var v = indexSize == 2 ? GetU16(raw, dataStart + j * 2) : GetU32(raw, dataStart + j * 4);
                    if (v >= n)
                    {
                        valid = false;
                        break;
                    }
                    indices[j] = v;
                }
                if (!valid)
                    continue;

                // A zero-filled pad can satisfy the header and the "< vertexCount" check.
                // Every index is 0, SharpGLTF drops the degenerate triangles, and the GLB
                // is an empty scene. Keep scanning for a buffer that actually addresses verts.
                if (n > 1)
                {
                    var maxIndex = 0u;
                    for (var j = 0; j < indices.Length; j++)
                        if (indices[j] > maxIndex) maxIndex = indices[j];
                    if (maxIndex == 0)
                        continue;
                }

                chunks.Add(indices);
                chunkOffsets.Add(x);
                x = dataEnd;
            }

            if (chunks.Count > 0)
                break; // stock width matched real chunks; don't reinterpret the region wider
        }

        if (chunks.Count == 0)
            throw new NotSupportedException($"Ghostwire static mesh LOD {index}: no index buffer found");

        var firstChunk = chunkOffsets[0];

        var table = ReadSectionTable(raw, blocks[index].Marker, chunks[0].Length, n);
        var indexBuffer = PickRenderIndexBuffer(chunks, table);

        var sections = new FStaticMeshSection[Math.Max(table.Count, 1)];
        if (table.Count == 0)
        {
            sections[0] = new FStaticMeshSection
            {
                MaterialIndex = 0,
                FirstIndex = 0,
                NumTriangles = indexBuffer.Length / 3,
                MinVertexIndex = 0,
                MaxVertexIndex = n - 1,
                bCastShadow = true,
                bVisibleInRayTracing = true
            };
        }
        for (var i = 0; i < table.Count; i++)
        {
            var record = table[i];
            sections[i] = new FStaticMeshSection
            {
                MaterialIndex = record.MaterialIndex,
                FirstIndex = record.FirstIndex,
                NumTriangles = record.NumTriangles,
                MinVertexIndex = record.MinVertexIndex,
                MaxVertexIndex = record.MaxVertexIndex,
                bEnableCollision = record.EnableCollision,
                bCastShadow = record.CastShadow,
                bForceOpaque = record.ForceOpaque,
                bVisibleInRayTracing = record.VisibleInRayTracing,
                bAffectDistanceFieldLighting = false
            };
        }

        // Tangent frame: (u32 8)(u32 N) followed by N * (sbyte3 tangent, sbyte handedness)(sbyte3 normal, sbyte).
        // UVs follow it as (u32 elementSize)(u32 N * TexCoordCount) plus vertex-major pairs, half
        // precision at 4 bytes an element and full precision at 8, so the channel count is declared
        // rather than inferred. The preamble of some assets also reads as an (8, N) pair; only the
        // real tangent stream is followed by the UV header, which settles which one to use.
        var frameStart = -1;
        var uvSetCount = 0;
        var uvStart = 0;
        var uvElementSize = 0;
        for (var o = scanStart; o + 8 <= firstChunk; o += 2)
        {
            if (GetU32(raw, o) != 8 || GetU32(raw, o + 4) != (uint)n)
                continue;

            var candidate = o + 8;
            if (candidate + 8L * n > raw.Length)
                break;
            if (frameStart < 0)
                frameStart = candidate;

            var header = candidate + 8L * n;
            if (header + 8 > firstChunk)
                continue;

            var element = GetU32(raw, (int)header);
            var count = GetU32(raw, (int)header + 4);
            if (element is not (4 or 8) || count == 0 || count % (uint)n != 0 || count / n > MAX_STATIC_UV_SETS_UE4)
                continue;
            if (header + 8 + element * (long)count > firstChunk)
                continue;

            frameStart = candidate;
            uvSetCount = (int)(count / n);
            uvStart = (int)header + 8;
            uvElementSize = (int)element;
            break;
        }

        var uv = new float[n * Math.Max(uvSetCount, 1) * 2];
        for (var i = 0; i < n; i++)
        {
            for (var set = 0; set < uvSetCount; set++)
            {
                var p = uvStart + (i * uvSetCount + set) * uvElementSize;
                uv[set * n * 2 + i * 2] = uvElementSize == 4
                    ? (float)BitConverter.UInt16BitsToHalf(GetU16(raw, p))
                    : BitConverter.ToSingle(raw, p);
                uv[set * n * 2 + i * 2 + 1] = uvElementSize == 4
                    ? (float)BitConverter.UInt16BitsToHalf(GetU16(raw, p + 2))
                    : BitConverter.ToSingle(raw, p + 4);
            }
        }
        if (uvSetCount == 0)
            uvSetCount = 1;

        var colorVertexBuffer = TryReadColorVertexBuffer(raw, n, uvStart + uvSetCount * n * uvElementSize, firstChunk);

        var normals = new FVector[n];
        var tangents = new FVector[n];
        var handedness = new byte[n];
        if (frameStart >= 0)
        {
            for (var i = 0; i < n; i++)
            {
                var p = frameStart + i * 8;
                tangents[i] = UnpackDirection(raw, p);
                normals[i] = UnpackDirection(raw, p + 4);
                handedness[i] = (sbyte)raw[p + 3] < 0 ? (byte)0x00 : (byte)0xFF;
            }
        }
        else
        {
            ComputeTangentFrame(verts, indexBuffer, uv, normals, tangents, handedness);
        }

        var uvItems = new FStaticMeshUVItem[n];
        for (var i = 0; i < n; i++)
        {
            var uvSets = new FMeshUVFloat[uvSetCount];
            for (var set = 0; set < uvSetCount; set++)
                uvSets[set] = new FMeshUVFloat(uv[i * 2 + set * n * 2], uv[i * 2 + 1 + set * n * 2]);
            uvItems[i] = new FStaticMeshUVItem(
                [Pack(tangents[i], handedness[i]), new FPackedNormal(0), Pack(normals[i], 0xFF)], uvSets);
        }

        var vertexBuffer = new FStaticMeshVertexBuffer(uvSetCount, n, uvItems);

        return new FStaticMeshLODResources(
            sections,
            new FPositionVertexBuffer(verts, 12),
            vertexBuffer,
            new FRawStaticIndexBuffer(indexBuffer),
            colorVertexBuffer);
    }

    private static FColorVertexBuffer? TryReadColorVertexBuffer(byte[] raw, int vertexCount, int streamStart, int streamEnd)
    {
        if (streamStart < 0 || streamStart + 8 > streamEnd || streamEnd > raw.Length)
            return null;

        for (var offset = streamStart; offset + 8 <= streamEnd; offset += 4)
        {
            var itemSize = (int)GetU32(raw, offset);
            var count = (int)GetU32(raw, offset + 4);
            if (itemSize != 4 || count != vertexCount)
                continue;

            var end = offset + 8 + count * itemSize;
            if (end > streamEnd)
                continue;

            var data = new FColor[count];
            for (var i = 0; i < count; i++)
            {
                var p = offset + 8 + i * itemSize;
                data[i] = new FColor(raw[p], raw[p + 1], raw[p + 2], raw[p + 3]);
            }

            return new FColorVertexBuffer(data);
        }

        return null;
    }

    // A LOD is preceded by its section table: (u32 SectionCount)(SectionCount * record), the last
    // record ending 14 bytes ahead of the LOD's position marker. A record is nine u32: material
    // index, first index, triangle count, min and max vertex index, then the collision, shadow,
    // opaque and ray tracing flags. Records are contiguous over the index buffer, so the table is
    // read back to front from the marker, which also verifies the layout.
    private List<SectionRecord> ReadSectionTable(byte[] raw, int marker, int indexCount, int n)
    {
        const int recordSize = 9 * 4;
        var slots = Math.Max(_materialSlots, 1);
        var records = new List<SectionRecord>();
        var expectedEnd = indexCount;

        for (var o = marker - 14 - recordSize; o >= 0; o -= recordSize)
        {
            var material = (int)GetU32(raw, o);
            var firstIndex = (int)GetU32(raw, o + 4);
            var triangles = (int)GetU32(raw, o + 8);
            var minVertex = (int)GetU32(raw, o + 12);
            var maxVertex = (int)GetU32(raw, o + 16);
            if (material < 0 || material >= slots || firstIndex < 0 || triangles < 1 || triangles > 4_000_000)
                break;
            if (firstIndex + triangles * 3 != expectedEnd || minVertex < 0 || minVertex > maxVertex || maxVertex >= n)
                break;

            var flags = 0;
            for (var f = 0; f < 4; f++)
            {
                var value = GetU32(raw, o + 20 + f * 4);
                if (value > 1)
                {
                    flags = -1;
                    break;
                }
                flags |= (int)value << f;
            }
            if (flags < 0)
                break;

            records.Add(new SectionRecord(material, firstIndex, triangles, minVertex, maxVertex,
                (flags & 1) != 0, (flags & 2) != 0, (flags & 4) != 0, (flags & 8) != 0));
            expectedEnd = firstIndex;
            if (firstIndex == 0)
                break;
        }

        records.Reverse();

        // The first LOD's table starts ahead of the render data stream, so its leading record can be
        // clipped away. What it held is implied by the record that follows it.
        if (records.Count > 0 && records[0].FirstIndex > 0)
        {
            var next = records[0];
            if (next.FirstIndex % 3 != 0)
                return [];

            var material = 0;
            for (var slot = 0; slot < slots; slot++)
            {
                var used = false;
                foreach (var record in records)
                    used |= record.MaterialIndex == slot;
                if (!used)
                {
                    material = slot;
                    break;
                }
            }

            records.Insert(0, new SectionRecord(material, 0, next.FirstIndex / 3, 0,
                next.MinVertexIndex > 0 ? next.MinVertexIndex - 1 : n - 1,
                next.EnableCollision, next.CastShadow, next.ForceOpaque, next.VisibleInRayTracing));
        }

        var total = 0;
        foreach (var record in records)
            total += record.NumTriangles * 3;
        return total == indexCount ? records : [];
    }

    // Every LOD ships the render index buffer plus a depth-only copy that draws the same triangles
    // through a different vertex range. Only the render buffer keeps each section inside the vertex
    // range the section table declares for it.
    private static uint[] PickRenderIndexBuffer(List<uint[]> chunks, List<SectionRecord> table)
    {
        var best = chunks[0];
        var bestScore = -1;
        foreach (var chunk in chunks)
        {
            var score = 0;
            foreach (var record in table)
            {
                var end = record.FirstIndex + record.NumTriangles * 3;
                if (end > chunk.Length)
                    continue;

                var inside = true;
                for (var i = record.FirstIndex; i < end && inside; i++)
                    inside = chunk[i] >= record.MinVertexIndex && chunk[i] <= record.MaxVertexIndex;
                if (inside)
                    score++;
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = chunk;
            }
        }
        return best;
    }

    private static FVector UnpackDirection(byte[] raw, int o)
    {
        var x = (sbyte)raw[o] / 127f;
        var y = (sbyte)raw[o + 1] / 127f;
        var z = (sbyte)raw[o + 2] / 127f;
        var length = MathF.Sqrt(x * x + y * y + z * z);
        return length > 1e-6f ? new FVector(x / length, y / length, z / length) : new FVector(0, 0, 1);
    }

    private static FPackedNormal Pack(FVector v, byte w)
    {
        static uint Component(float value) => (uint)Math.Clamp((int)MathF.Round((value + 1) * 127.5f), 0, 255);
        return new FPackedNormal(Component(v.X) | (Component(v.Y) << 8) | (Component(v.Z) << 16) | ((uint)w << 24));
    }

    // Fallback for assets without a tangent frame stream. Unreal winds triangles so that the face
    // normal is the reverse of the edge cross product.
    private static void ComputeTangentFrame(FVector[] verts, uint[] indices, float[] uv, FVector[] normals, FVector[] tangents, byte[] handedness)
    {
        var n = verts.Length;
        var normalSum = new FVector[n];
        var tangentSum = new FVector[n];
        for (var t = 0; t + 2 < indices.Length; t += 3)
        {
            int i0 = (int)indices[t], i1 = (int)indices[t + 1], i2 = (int)indices[t + 2];
            var a = verts[i1] - verts[i0];
            var b = verts[i2] - verts[i0];
            var face = new FVector(
                a.Z * b.Y - a.Y * b.Z,
                a.X * b.Z - a.Z * b.X,
                a.Y * b.X - a.X * b.Y);
            normalSum[i0] += face; normalSum[i1] += face; normalSum[i2] += face;

            var du1 = uv[i1 * 2] - uv[i0 * 2];
            var dv1 = uv[i1 * 2 + 1] - uv[i0 * 2 + 1];
            var du2 = uv[i2 * 2] - uv[i0 * 2];
            var dv2 = uv[i2 * 2 + 1] - uv[i0 * 2 + 1];
            var determinant = du1 * dv2 - du2 * dv1;
            if (Math.Abs(determinant) <= 1e-8f)
                continue;

            var tangent = (a * dv2 - b * dv1) / determinant;
            tangentSum[i0] += tangent; tangentSum[i1] += tangent; tangentSum[i2] += tangent;
        }

        for (var i = 0; i < n; i++)
        {
            var normal = normalSum[i];
            var length = MathF.Sqrt(normal.X * normal.X + normal.Y * normal.Y + normal.Z * normal.Z);
            normals[i] = length > 1e-8f ? normal / length : new FVector(0, 0, 1);

            var tangent = tangentSum[i] - normals[i] * FVector.DotProduct(tangentSum[i], normals[i]);
            length = MathF.Sqrt(tangent.X * tangent.X + tangent.Y * tangent.Y + tangent.Z * tangent.Z);
            tangents[i] = length > 1e-8f
                ? tangent / length
                : FVector.CrossProduct(MathF.Abs(normals[i].Z) < 0.9f ? new FVector(0, 0, 1) : new FVector(1, 0, 0), normals[i]);
            handedness[i] = 0xFF;
        }
    }

    private static FBoxSphereBounds ComputeBounds(FStaticMeshLODResources[] lods)
    {
        var min = new float[3] { float.MaxValue, float.MaxValue, float.MaxValue };
        var max = new float[3] { float.MinValue, float.MinValue, float.MinValue };
        var hasAny = false;

        foreach (var lod in lods)
        {
            var v = lod.PositionVertexBuffer?.Verts;
            if (v is null)
                continue;
            foreach (var p in v)
            {
                hasAny = true;
                if (p.X < min[0]) min[0] = p.X; if (p.X > max[0]) max[0] = p.X;
                if (p.Y < min[1]) min[1] = p.Y; if (p.Y > max[1]) max[1] = p.Y;
                if (p.Z < min[2]) min[2] = p.Z; if (p.Z > max[2]) max[2] = p.Z;
            }
        }

        if (!hasAny)
            return new FBoxSphereBounds(new FVector(0), new FVector(0), 0f);

        var origin = new FVector((min[0] + max[0]) / 2f, (min[1] + max[1]) / 2f, (min[2] + max[2]) / 2f);
        var extent = new FVector((max[0] - min[0]) / 2f, (max[1] - min[1]) / 2f, (max[2] - min[2]) / 2f);
        var radius = (float)Math.Sqrt(extent.X * extent.X + extent.Y * extent.Y + extent.Z * extent.Z);
        return new FBoxSphereBounds(origin, extent, radius);
    }

    private static ushort GetU16(byte[] b, int o) => (ushort)(b[o] | (b[o + 1] << 8));
    private static uint GetU32(byte[] b, int o) => (uint)(b[o] | (b[o + 1] << 8) | (b[o + 2] << 16) | (b[o + 3] << 24));
}
