using System;
using System.Buffers.Binary;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Serilog;

namespace CUE4Parse.GameTypes.HonorOfKings.Lua;

public class FNGRLuaArchive(string name, byte[] data, VersionContainer? versions = null) : FByteArchive(name, data, versions)
{
    public T ReadBE<T>() where T : unmanaged
    {
        T value = Read<T>();

        return value switch
        {
            ushort v => (T) (object) BinaryPrimitives.ReverseEndianness(v),
            uint v => (T) (object) BinaryPrimitives.ReverseEndianness(v),
            ulong v => (T) (object) BinaryPrimitives.ReverseEndianness(v),
            short v => (T) (object) BinaryPrimitives.ReverseEndianness(v),
            int v => (T) (object) BinaryPrimitives.ReverseEndianness(v),
            long v => (T) (object) BinaryPrimitives.ReverseEndianness(v),
            _ => value 
        };
    }
}

public readonly struct FadeFaceHeader
{
    public readonly uint Magic;
    public readonly uint Size;
    public readonly ulong Hash;
    public readonly ushort StartIndex;
    public readonly ushort ChunkSize;

    public FadeFaceHeader(FNGRLuaArchive Ar)
    {
        if (Ar.Length < 0x14)
            throw new ArgumentException("Fade Face header is too small");

        Magic = Ar.ReadBE<uint>();
        Size = Ar.ReadBE<uint>();
        Hash = Ar.ReadBE<ulong>();
        StartIndex = Ar.ReadBE<ushort>();
        ChunkSize = Ar.ReadBE<ushort>();
    }
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
        Header = new FadeFaceHeader(Ar);

        if (Header.Magic != NGR_LUA_MAGIC)
        {
            Log.Error($"Invalid magic: 0x{Header.Magic:X}, expected: 0x{NGR_LUA_MAGIC:X}");
            result = data;
            return;
        }

        ReadChunks(Ar);
        Reorder();
        result = Rebuild();
        //result = Restore(result); // TODO: Opcode is still shuffled
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
        for (int i = 0; i < toCopy; i++)
        {
            int index = bytesWritten + i;
            result[index] = (byte) (chunk.Data[i] ^ _decryptionKey[index % Math.Min(_decryptionKey.Length, 32)]); // Always clamped to 32 even when chunk size is larger
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
}
