using System.Reflection;
using System.Resources;
using System.Security.Cryptography;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.GameTypes.Mars.Encryption;

public static class MarsDecrypt
{
    private static readonly RSA _rsa;

    static MarsDecrypt()
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

    public static FArchive DecryptUassetArchive(FArchive Ar)
    {
        if (Ar.Peek<uint>() != 0x34B8D695)
            return Ar;

        Ar.Position += sizeof(uint);
        var blockCount = Ar.Read<int>();

        using var ms = new MemoryStream();
        for (int i = 0; i < blockCount; i++)
        {
            var blockSize = Ar.Read<int>();
            var decrypted = _rsa.Decrypt(Ar.ReadSpan(blockSize), RSAEncryptionPadding.Pkcs1);
            ms.Write(decrypted, 0, decrypted.Length);
        }

        Ar.Position = 0;
        var bytes = Ar.ReadBytes((int)Ar.Length);
        ms.Position = 0;
        ms.GetBuffer().AsSpan()[..(int)ms.Length].CopyTo(bytes.AsSpan());

        return new FByteArchive(Ar.Name, bytes, Ar.Versions);
    }
}
