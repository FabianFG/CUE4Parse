using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using CUE4Parse.UE4.Lua;
using CUE4Parse.UE4.Versions;
using Serilog;

namespace CUE4Parse.GameTypes.HonorOfKings.Lua;

public class FNGRLuaArchive(string name, byte[] data, VersionContainer? versions = null) : FLuaArchive(name, data, versions)
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

    private static readonly Dictionary<byte, byte> _opcodeMapping = new()
    {
        { 0x0A, 0x00 },
        { 0x00, 0x0A },
        { 0x07, 0x03 },
        { 0x03, 0x07 },
        { 0x01, 0x09 },
        { 0x09, 0x01 },
        { 0x02, 0x08 },
        { 0x08, 0x02 },
        { 0x04, 0x06 },
        { 0x06, 0x04 },
    };

    public static byte[] DecryptLua(string name, byte[] data)
    {
        var reader = new NGRLuaReader();
        return reader.DecryptLuaInternal(name, data);
    }

    public byte[] DecryptLuaInternal(string name, byte[] data)
    {
        using var Ar = new FNGRLuaArchive(name, data, null);
        if (Ar.Length < 0x14)
        {
            Log.Warning("Fade Face header is too small");
            return data;
        }

        Header = new FadeFaceHeader(Ar);
        if (Header.Magic != NGR_LUA_MAGIC)
        {
            Log.Warning($"Invalid magic: 0x{Header.Magic:X}, expected: 0x{NGR_LUA_MAGIC:X}");
            return data;
        }

        ReadChunks(Ar);
        Reorder();
        return Restore(name, Rebuild());
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

        if (!FLuaReader.IsValidLuaMagic(data))
            throw new InvalidDataException("Failed to decrypt. Expected Lua magic");

        return data;
    }

    private static byte[] Restore(string name, byte[] decryptedLuaBytecode)
    {
        using var Ar = new FNGRLuaArchive(name, decryptedLuaBytecode, null);

        var lua = FLuaReader.ReadLua54(Ar, _opcodeMapping);

        using var msOut = new MemoryStream(decryptedLuaBytecode.Length);
        using (var writer = new FLuaArchiveWriter(msOut))
        {
            FLuaWriter54.Write(writer, lua);
            writer.Flush();
        }

        return msOut.ToArray();
    }
}
