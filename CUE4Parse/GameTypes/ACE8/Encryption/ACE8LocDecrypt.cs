using System.IO.Compression;
using System.Text;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.GameTypes.ACE8.Encryption;

public static class ACE8LocDecrypt
{
    private struct FACE8StringPart
    {
        public int Length;
        public int Position;
        public int Id;
        public int ChildCount;

        public FACE8StringPart(FArchive Ar)
        {
            Length = Ar.Read<int>();
            Position = (int)Ar.Position;
            Ar.Position += Length;
            Id = Ar.Read<int>();
            ChildCount = Ar.Read<int>();
        }
    }

    public static string[] ReadDatFile(GameFile file)
    {
        if (!file.Path.Contains("Localization/GameData") || file.NameWithoutExtension.Contains("_Cmn")) return [];
        var decompressed = DecompressDatFile(file);
        var data = decompressed.GetBuffer().AsSpan()[..(int)decompressed.Length];

        var strings = new List<string>(8192);
        while (!data.IsEmpty)
        {
            var end = data.IndexOf((byte)0);
            if (end < 0)
                break;

            strings.Add(Encoding.UTF8.GetString(data[..end]));
            data = data[(end + 1)..];
        }

        return strings.ToArray();
    }

    public static Dictionary<int, string> ReadKeysDatFile(GameFile file)
    {
        if (!file.Path.Contains("Localization/GameData") || !file.NameWithoutExtension.Contains("_Cmn")) return [];
        var decompressed = DecompressDatFile(file);
        var data = decompressed.GetBuffer();

        using var Ar = new FByteArchive("", decompressed.GetBuffer(), decompressed.Length);
        var header = Ar.ReadArray<int>(4);
        var entries = new List<FACE8StringPart>();
        while (Ar.Position < Ar.Length)
        {
            entries.Add(new FACE8StringPart(Ar));
        }

        byte[] buffer = new byte[256];
        var keys = new Dictionary<int, string>(8192);
        var index = 0;
        void Collect(int length)
        {
            var part = entries[index++];
            data.AsSpan().Slice(part.Position, part.Length).CopyTo(buffer.AsSpan()[length..]);
            length += part.Length;

            if (part.Id != -1)
                keys[part.Id] = Encoding.UTF8.GetString(buffer.AsSpan()[..length]);

            for (var i = 0; i < part.ChildCount; i++)
                Collect(length);
        }

        var rootCount = entries.Count - entries.Sum(x => x.ChildCount);
        for (var i = 0; i < rootCount; i++)
            Collect(0);

        return keys.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
    }

    private static MemoryStream DecompressDatFile(GameFile file)
    {
        using var compressed = new MemoryStream(DecryptDatFile(file));
        using var zlibStream = new ZLibStream(compressed, CompressionMode.Decompress);
        var decompressed = new MemoryStream();
        zlibStream.CopyTo(decompressed);
        return decompressed;
    }

    /// <summary>
    /// Reverse by EmOo
    /// </summary>
    private static byte[] DecryptDatFile(GameFile file)
    {
        var name = file.NameWithoutExtension;
        var langIdx = name.Contains("_Cmn") ? 0 : name[name.IndexOf('_') + 1] - 'A';

        var result = file.Read();
        uint state = 0;
        uint key = (uint)(result.Length + langIdx);

        for (var i = 0; i < result.Length; i++)
        {
            var x = (state * 8) ^ state;
            state = ((~x) >> 7) & 1 | state * 2;
            key = key * 5 + 1;
            result[i] ^= (byte)(key + state);
        }

        return result;
    }
}
