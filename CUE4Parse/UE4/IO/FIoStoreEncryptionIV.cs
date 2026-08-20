using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO;

public class FIoStoreEncryptionIV
{
    public byte[] Bytes;

    public FIoStoreEncryptionIV(FArchive Ar)
    {
        Bytes = Ar.ReadBytes(12);
    }
}

public enum EIoEncryptionMethod : byte
{
    None	= 0,
    AES 	= (1 << 0),	// ECB
    AES_CTR	= (1 << 1)
}