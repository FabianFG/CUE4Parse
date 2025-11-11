using System;
using System.IO;

namespace CUE4Parse.UE4.CriWare.Decoders;

public class CriwareDecryptionException : Exception
{
    public CriwareDecryptionException(string message) : base(message) { }
    public CriwareDecryptionException(string message, Exception inner) : base(message, inner) { }
}

// In order not to pass subkey through various methods I embedded it directly into audio data
public static class CriCipherUtil
{
    private const int SubKeySize = sizeof(ushort);

    public static byte[] EmbedSubKey(this Stream stream, ushort subKey)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var memoryStream = new MemoryStream((int) (stream.Length + sizeof(ushort)));
        stream.CopyTo(memoryStream);
        memoryStream.Write(BitConverter.GetBytes(subKey));
        return memoryStream.GetBuffer();
    }

    public static (ushort subKey, byte[] audioData) ExtractSubKey(this byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length < SubKeySize)
            throw new InvalidDataException("HCA data too short to contain a subkey.");

        int subKeyOffset = data.Length - SubKeySize;
        ushort subKey = BitConverter.ToUInt16(data, subKeyOffset);

        var audioData = new byte[subKeyOffset];
        Buffer.BlockCopy(data, 0, audioData, 0, subKeyOffset);
        return (subKey, audioData);
    }
}
