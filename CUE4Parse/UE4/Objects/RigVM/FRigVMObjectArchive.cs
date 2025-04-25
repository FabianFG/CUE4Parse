using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.RigVM;

public class FRigVMObjectArchive
{
    public byte[] Buffer;
    public int UncompressedSize;
    public int CompressedSize;
    public bool bIsCompressed;

    public FRigVMObjectArchive(FArchive Ar)
    {
        Buffer = Ar.ReadArray<byte>();
        UncompressedSize = Ar.Read<int>();
        CompressedSize = Ar.Read<int>();
        bIsCompressed = Ar.ReadBoolean();
    }
}
