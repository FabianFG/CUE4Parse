using System.Reflection;
using System.Resources;
using System.Security.Cryptography;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.GameTypes.Mars.Encryption;

public class MarsDecrypt
{
    private readonly RSA _rsa;

    public MarsDecrypt()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CUE4Parse.Resources.MarsKey.bin");
        if (stream == null)
        {
            throw new MissingManifestResourceException("Couldn't find MarsKey.bin in Embedded Resources");
        }

        var derBytes = new byte[stream.Length];
        stream.ReadExactly(derBytes, 0, (int)stream.Length);

        _rsa = RSA.Create();
        _rsa.ImportPkcs8PrivateKey(derBytes, out _);
    }

    public FArchive DecryptUassetArchive(FArchive Ar)
    {
        var originalBytes = Ar.ReadBytes((int)Ar.Length);

        var tag = BitConverter.ToUInt32(originalBytes, 0);
        if (tag != 0x34B8D695)
        {
            // normal package, no encryption.
            return new FByteArchive(Ar.Name, originalBytes, Ar.Versions);
        }

        int pos = 4;
        int blockCount = BitConverter.ToInt32(originalBytes, pos);
        pos += 4;

        var decryptedParts = new byte[blockCount][];
        int totalDecryptedLength = 0;

        for (int i = 0; i < blockCount; i++)
        {
            if (pos + 4 > originalBytes.Length)
                throw new InvalidDataException("truncated block size.");

            int blockSize = BitConverter.ToInt32(originalBytes, pos);
            pos += 4;

            if (pos + blockSize > originalBytes.Length)
                throw new InvalidDataException("truncated ciphertext.");

            byte[] ciphertext = new byte[blockSize];
            Array.Copy(originalBytes, pos, ciphertext, 0, blockSize);
            pos += blockSize;

            byte[] plaintext = _rsa.Decrypt(ciphertext, RSAEncryptionPadding.Pkcs1);
            decryptedParts[i] = plaintext;
            totalDecryptedLength += plaintext.Length;
        }

        var decryptedSummary = new byte[totalDecryptedLength];
        int offset = 0;
        foreach (var part in decryptedParts)
        {
            Array.Copy(part, 0, decryptedSummary, offset, part.Length);
            offset += part.Length;
        }

        var fullDecrypted = new byte[originalBytes.Length];
        Array.Copy(originalBytes, fullDecrypted, originalBytes.Length);
        Array.Copy(decryptedSummary, 0, fullDecrypted, 0, decryptedSummary.Length);

        return new FByteArchive(Ar.Name, fullDecrypted, Ar.Versions);
    }
}
