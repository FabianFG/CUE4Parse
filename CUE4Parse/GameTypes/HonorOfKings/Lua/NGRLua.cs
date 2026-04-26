using System;
using System.IO;
using CUE4Parse.UE4.Lua;
using Serilog;

namespace CUE4Parse.GameTypes.HonorOfKings.Lua;

public readonly struct FadeFaceHeader(FNGRLuaArchive Ar)
{
    public readonly uint Magic = Ar.ReadBE<uint>();
    public readonly uint Size = Ar.ReadBE<uint>();
    public readonly ulong Hash = Ar.ReadBE<ulong>();
    public readonly ushort StartIndex = Ar.ReadBE<ushort>();
    public readonly ushort ChunkSize = Ar.ReadBE<ushort>();
}

public readonly struct Chunk(FNGRLuaArchive Ar, int chunkSize)
{
    public readonly byte XorByte = Ar.Read<byte>();
    public readonly short Step = Ar.ReadBE<short>();
    public readonly byte[] Data = Ar.ReadBytes(chunkSize);
}

public class NGRLuaReader
{
    private const uint NGR_LUA_MAGIC = 0xFADEFACE;

    public FadeFaceHeader Header;

    private Chunk[] _luaChunks = [];
    private byte[] _decryptionKey = [];

    public NGRLuaReader(string name, byte[] data, out byte[] result)
    {
        var Ar = new FNGRLuaArchive(name, data, null);
        if (Ar.Length < 0x14)
        {
            Log.Warning("Fade Face header is too small");
            result = data;
            return;
        }

        Header = new FadeFaceHeader(Ar);
        if (Header.Magic != NGR_LUA_MAGIC)
        {
            Log.Warning($"Invalid magic: 0x{Header.Magic:X}, expected: 0x{NGR_LUA_MAGIC:X}");
            result = data;
            return;
        }

        ReadChunks(Ar);
        Reorder();
        result = Restore(name, Rebuild());
    }

    private void ReadChunks(FNGRLuaArchive Ar)
    {
        var chunksCount = (int) ((Header.Size + Header.ChunkSize - 1) / Header.ChunkSize);
        _luaChunks = Ar.ReadArray(chunksCount, () => new Chunk(Ar, Header.ChunkSize));
    }

    private void Reorder()
    {
        var sorted = new Chunk[_luaChunks.Length];
        _decryptionKey = new byte[_luaChunks.Length];
        var visited = new bool[_luaChunks.Length];

        int cur = Header.StartIndex;
        for (int i = 0; i < _luaChunks.Length; i++)
        {
            if (cur < 0 || cur >= _luaChunks.Length)
                throw new IndexOutOfRangeException($"Index out of range at step {i}: {cur}");
            if (visited[cur])
                throw new Exception($"Cycle detected at index {cur}");

            visited[cur] = true;
            sorted[i] = _luaChunks[cur];
            _decryptionKey[i] = sorted[i].XorByte;

            cur += sorted[i].Step;
        }

        _luaChunks = sorted;
    }

    private void Decrypt(Chunk chunk, byte[] result, int bytesWritten, int toCopy)
    {
        var clamp = Math.Min(_decryptionKey.Length, 32);
        for (int i = 0; i < toCopy; i++)
        {
            int index = bytesWritten + i;
            result[index] = (byte) (chunk.Data[i] ^ _decryptionKey[index % clamp]); // Always clamped to 32 even when chunk size is larger
        }
    }

    private byte[] Rebuild()
    {
        var data = new byte[Header.Size];
        int bytesWritten = 0;

        foreach (var chunk in _luaChunks)
        {
            // We essentially trim leftover bytes from the last chunk if it exceeds the expected size
            var remaining = (int) Header.Size - bytesWritten;
            var toCopy = Math.Min(chunk.Data.Length, remaining);
            if (toCopy > 0)
            {
                Decrypt(chunk, data, bytesWritten, toCopy);
                bytesWritten += toCopy;
            }
        }

        if (data.Length < 4)
            throw new Exception("Data too short");

        if (data is not [0x1B, 0x4C, 0x75, 0x61, ..]) // "\x1BLua"
            throw new Exception("Failed to decrypt. Expected Lua magic");

        return data;
    }

    private static byte[] Restore(string name, byte[] decryptedLuaBytecode)
    {
        using var Ar = new FNGRLuaArchive(name, decryptedLuaBytecode, null);
        var lua = new LuaBytecode(Ar);

        using var msOut = new MemoryStream(decryptedLuaBytecode.Length);
        using (var writer = new FLuaArchiveWriter(msOut))
        {
            FLuaWriter54.Write(writer, lua);
            writer.Flush();
        }

        return msOut.ToArray();
    }
}
