using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO;

public class FIoStoreEncryptionIV
{
    public const int Size = 12;
    public byte[] Bytes;

    public FIoStoreEncryptionIV(FArchive Ar)
    {
        Bytes = Ar.ReadBytes(Size);
    }

    public FIoStoreEncryptionIV(byte[] bytes)
    {
        if (bytes.Length != Size)
            throw new ArgumentException($"Encryption IV must be exactly {Size} bytes.", nameof(bytes));
        Bytes = bytes;
    }
}

public enum EIoEncryptionMethod : byte
{
    None	= 0,
    AES 	= (1 << 0),	// ECB
    AES_CTR	= (1 << 1)
}
