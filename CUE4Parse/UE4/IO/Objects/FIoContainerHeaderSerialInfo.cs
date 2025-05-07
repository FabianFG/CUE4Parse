using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects;

public class FIoContainerHeaderSerialInfo
{
    public long Offset;
    public long Size;

    public FIoContainerHeaderSerialInfo(FArchive Ar)
    {
        Offset = Ar.Read<long>();
        Size = Ar.Read<long>();
    }
}
