using CUE4Parse.UE4.FMod.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CUE4Parse.UE4.FMod.Objects;

// Example usage:
// var guidString = "5e136771-13a0-4020-b14c-cdb0efa627b0";
// var guid = new FModGuid(guidString);

// if (fmodReader.StringData?.RadixTree is { } tree &&
//     tree.TryGetString(guid, out var path))
// {
//     Log.Information($"GUID {guid} -> {path}");
// }
// else
// {
//     Log.Warning($"Could not resolve GUID {guid}.");
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
            LeafIndices = FModReader.ReadSimpleArray24(Ar);
            ParentIndices = FModReader.ReadSimpleArray24(Ar);
        }

        for (int i = 0; i < Guids.Length; i++)
            _guidIndex[Guids[i]] = i;
    }

    #region Readers
    private static byte[] ReadByteArray(BinaryReader Ar)
    {
        uint count = FModReader.ReadX16(Ar);
        var data = Ar.ReadBytes((int)count);
        return data;
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
        if (offset < 0 || offset >= StringBlob.Length) return string.Empty;

        int end = offset;
        while (end < StringBlob.Length && StringBlob[end] != 0)
        {
            end++;
        }

        return Encoding.UTF8.GetString(StringBlob, offset, end - offset);
    }

    #endregion
}
