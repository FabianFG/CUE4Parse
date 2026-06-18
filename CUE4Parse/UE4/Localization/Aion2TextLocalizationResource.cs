using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CUE4Parse.UE4.Exceptions;
using K4os.Compression.LZ4;

namespace CUE4Parse.UE4.Localization;

public static class Aion2TextLocalizationResource
{
    private static readonly byte[] KeyManifestMaterial =
    [
        0x9c, 0x9e, 0x42, 0x21, 0x0f, 0x2f, 0x5f, 0xbf,
        0x03, 0x0d, 0xa9, 0xab, 0xe9, 0xef, 0xa9, 0xab,
        0x6e, 0x73, 0x26, 0x35, 0x48, 0x5d, 0x7a, 0x6f,
        0x14, 0x09, 0xe4, 0x94, 0xcb, 0xfc, 0xbd, 0x4e
    ];

    public static Dictionary<string, string> Read(byte[] dat, byte[] keyManifest)
    {
        if (dat.Length < 0x18) throw new ParserException("AION2 L10N container is too small");
        var version = ReadU32(dat, 0);
        var seed = BitConverter.ToUInt64(dat, 4);
        var rawSize = checked((int) ReadU32(dat, 12));
        var packedSize = checked((int) ReadU32(dat, 16));
        var alignedSize = checked((int) ReadU32(dat, 20));
        if (version != 2 || rawSize <= 0 || packedSize <= 0 || alignedSize < packedSize || 0x18 + alignedSize > dat.Length)
            throw new ParserException("Invalid AION2 L10N container header");

        var key = FindContentKey(keyManifest, seed);
        var decrypted = AesEcb(dat.AsSpan(0x18, alignedSize), key);
        var raw = new byte[rawSize];
        var written = LZ4Codec.Decode(decrypted.AsSpan(0x10, packedSize), raw);
        if (written != rawSize) throw new ParserException($"AION2 L10N LZ4 decode failed ({written}/{rawSize})");

        var pos = 0;
        if (ReadI32(raw, ref pos) != 1) throw new ParserException("Invalid AION2 L10N table version");
        if (ReadFString(raw, ref pos) != "AION2") throw new ParserException("Invalid AION2 L10N namespace");
        var count = ReadI32(raw, ref pos);
        var result = new Dictionary<string, string>(count);
        for (var i = 0; i < count; i++)
        {
            var keyName = ReadFString(raw, ref pos);
            var value = ReadFString(raw, ref pos);
            result[keyName] = value;
        }
        return result;
    }

    public static byte[] ReadDataTable(byte[] dat, byte[] keyManifest)
    {
        if (dat.Length < 0x1C || ReadU32(dat, 0) != 13)
            throw new ParserException("Invalid AION2 data table container header");

        var key = FindContentKey(keyManifest, BitConverter.ToUInt64(dat, 8));
        if (ReadU32(dat, 4) == 2)
        {
            var rawSize = checked((int) ReadU32(dat, 16));
            var packedSize = checked((int) ReadU32(dat, 20));
            var alignedSize = checked((int) ReadU32(dat, 24));
            if (rawSize <= 0 || packedSize <= 0 || alignedSize < packedSize || 0x1C + alignedSize > dat.Length)
                throw new ParserException("Invalid AION2 compressed data table header");
            var decrypted = AesEcb(dat.AsSpan(0x1C, alignedSize), key);
            var raw = new byte[rawSize];
            var written = LZ4Codec.Decode(decrypted.AsSpan(0x10, packedSize), raw);
            if (written != rawSize) throw new ParserException($"AION2 data table LZ4 decode failed ({written}/{rawSize})");
            return raw;
        }

        if (ReadU32(dat, 4) != 3 || ReadU32(dat, 24) != dat.Length - 0x1C)
            throw new ParserException("Unsupported AION2 data table cipher");
        var counter = new byte[16];
        dat.AsSpan(16, 8).CopyTo(counter);
        var output = new byte[dat.Length - 0x1C];
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        using var encryptor = aes.CreateEncryptor(key, null);
        var stream = new byte[16];
        for (var offset = 0; offset < output.Length; offset += 16)
        {
            encryptor.TransformBlock(counter, 0, 16, stream, 0);
            var blockSize = Math.Min(16, output.Length - offset);
            for (var i = 0; i < blockSize; i++) output[offset + i] = (byte) (dat[0x1C + offset + i] ^ stream[i]);
            for (var i = 8; i < 16 && ++counter[i] == 0; i++) { }
        }
        return output;
    }

    public static Dictionary<string, string> ReadKeyManifest(byte[] keyManifest)
    {
        var records = DecryptKeyManifest(keyManifest, out var count);
        var result = new Dictionary<string, string>(count);
        for (var i = 0; i < count; i++)
        {
            var offset = i * 0x30;
            result[$"0x{BitConverter.ToUInt64(records, offset):X16}"] = Convert.ToHexString(records, offset + 8, 32);
        }
        return result;
    }

    private static byte[] FindContentKey(byte[] keyManifest, ulong seed)
    {
        var records = DecryptKeyManifest(keyManifest, out var count);
        for (var i = 0; i < count; i++)
        {
            var off = i * 0x30;
            if (BitConverter.ToUInt64(records, off) == seed)
                return records.AsSpan(off + 8, 32).ToArray();
        }
        throw new ParserException($"AION2 content key not found for hash {seed}");
    }

    private static byte[] DecryptKeyManifest(byte[] keyManifest, out int count)
    {
        if (keyManifest.Length < 12) throw new ParserException("AION2 key_manifest.dat is too small");
        var version = ReadU32(keyManifest, 0);
        count = checked((int) ReadU32(keyManifest, 4));
        var payloadSize = checked((int) ReadU32(keyManifest, 8));
        if (version != 2 || payloadSize != count * 0x30 || 12 + payloadSize > keyManifest.Length)
            throw new ParserException("Invalid AION2 key_manifest.dat header");

        using var hasher = Blake3.Hasher.New();
        hasher.Update(KeyManifestMaterial);
        var manifestKey = hasher.Finalize().AsSpan().ToArray();
        return AesEcb(keyManifest.AsSpan(12, payloadSize), manifestKey);
    }

    private static byte[] AesEcb(ReadOnlySpan<byte> data, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        using var decryptor = aes.CreateDecryptor(key, null);
        return decryptor.TransformFinalBlock(data.ToArray(), 0, data.Length);
    }

    private static uint ReadU32(byte[] b, int off) => BitConverter.ToUInt32(b, off);

    private static int ReadI32(byte[] b, ref int pos)
    {
        var value = BitConverter.ToInt32(b, pos);
        pos += 4;
        return value;
    }

    private static string ReadFString(byte[] b, ref int pos)
    {
        var length = ReadI32(b, ref pos);
        if (length == 0) return string.Empty;
        if (length < 0)
        {
            var bytes = checked(-length * 2);
            var s = Encoding.Unicode.GetString(b, pos, bytes - 2);
            pos += bytes;
            return s;
        }
        var value = Encoding.UTF8.GetString(b, pos, length - 1);
        pos += length;
        return value;
    }
}
