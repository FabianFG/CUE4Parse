using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CUE4Parse.UE4.FMod.Enums;

namespace CUE4Parse.UE4.FMod.Objects;

// Example usage:
// var guidString = "5e136771-13a0-4020-b14c-cdb0efa627b0";
// var guid = new FModGuid(guidString);

// if (fmodReader.StringData?.RadixTree is { } tree &&
//     tree.TryGetString(guid, out var path))
// {
//     Console.WriteLine($"GUID {guid} -> {path}");
// }
// else
// {
//     Console.WriteLine($"Could not resolve GUID {guid}.");
// }
public partial class FRadixTreePacked
{
    private const int Sentinel24 = 0xFFFFFF;

    public readonly FPackedNode[] Nodes;
    public readonly FModGuid[] Guids;
    public readonly byte[] StringBlob;
    public readonly FUInt24[] LeafIndices = [];
    public readonly FUInt24[] ParentIndices = [];

    private readonly Dictionary<FModGuid, int> _guidIndex = [];

    public FRadixTreePacked(BinaryReader Ar, EStringTableType type)
    {
        Nodes = FModReader.ReadElemListImp<FPackedNode>(Ar);
        Guids = FModReader.ReadElemListImp<FModGuid>(Ar);
        StringBlob = ReadByteArray(Ar);

        if (type is EStringTableType.StringTable_RadixTree_24Bit)
        {
            LeafIndices = ReadSimpleArray24(Ar);
            ParentIndices = ReadSimpleArray24(Ar);
        }

        for (int i = 0; i < Guids.Length; i++)
            _guidIndex[Guids[i]] = i;
    }

    #region Readers
    private static uint ReadUInt24(BinaryReader br)
    {
        int b0 = br.ReadByte();
        int b1 = br.ReadByte();
        int b2 = br.ReadByte();
        return (uint) (b0 | (b1 << 8) | (b2 << 16));
    }

    private static byte[] ReadByteArray(BinaryReader Ar)
    {
        uint count = FModReader.ReadX16(Ar);
        var data = Ar.ReadBytes((int) count);
        return data;
    }

    private static FUInt24[] ReadSimpleArray24(BinaryReader Ar)
    {
        uint count = FModReader.ReadX16(Ar);

        if (count == 0)
            return [];

        int totalBytes = checked((int) count * 3); // 3 bytes per entry
        byte[] raw = Ar.ReadBytes(totalBytes);

        if (raw.Length != totalBytes)
            throw new EndOfStreamException($"Expected {totalBytes} bytes, got {raw.Length}");

        var arr = new FUInt24[count];
        int o = 0;
        for (int i = 0; i < count; i++)
        {
            uint v = (uint) (raw[o] | (raw[o + 1] << 8) | (raw[o + 2] << 16));
            o += 3;
            arr[i] = new FUInt24(v);
        }
        return arr;
    }

    #endregion

    #region Get string from Radix Tree

    public bool TryGetString(FModGuid guid, out string path)
    {
        if (_guidIndex.TryGetValue(guid, out int idx))
        {
            TryGetStringByIndex(idx, out path);

            return true;
        }

        path = string.Empty;

        return false;
    }

    public bool TryGetStringByIndex(int guidIndex, out string path, int maxLen = int.MaxValue)
    {
        path = string.Empty;

        if (guidIndex < 0 || guidIndex >= Guids.Length)
            return false;

        if (LeafIndices.Length != Guids.Length)
            return false;

        if (ParentIndices.Length != Nodes.Length)
        {
            return false;
        }

        uint node = LeafIndices[guidIndex].Value;
        if (node == Sentinel24)
        {
            path = string.Empty;
            return true;
        }

        var segments = new List<string>(8);
        int guard = 0;

        while (node != Sentinel24)
        {
            if (node < 0 || node >= Nodes.Length)
                break;

            var n = Nodes[node];
            if (n.HasString)
            {
                string seg = ReadCString(n.StringOffset);
                segments.Add(seg);
            }

            if (node >= ParentIndices.Length)
                break;

            node = ParentIndices[node].Value;

            if (++guard > 100000)
                throw new InvalidDataException("Parent chain too long");
        }

        segments.Reverse();
        string full = string.Concat(segments);

        if (full.Length + 1 > maxLen)
        {
            path = full.Length >= maxLen - 1
                ? full[..Math.Max(0, maxLen - 1)]
                : full;

            return true; // It's truncated but we ball
        }

        path = full;
        return true;
    }

    private string ReadCString(int offset)
    {
        if (offset < 0 || offset >= StringBlob.Length)
            return string.Empty;

        int end = offset;
        while (end < StringBlob.Length && StringBlob[end] != 0)
        {
            end++;
        }

        return Encoding.UTF8.GetString(StringBlob, offset, end - offset);
    }

    #endregion
}
