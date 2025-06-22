using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkStatePropertyInfo
{
    public readonly int PropertyId;
    public readonly byte AccumType;
    public readonly byte InDb;

    public AkStatePropertyInfo(FArchive Ar)
    {
        PropertyId = WwiseReader.Read7BitEncodedIntBE(Ar);
        AccumType = Ar.Read<byte>();
        if (WwiseVersions.Version > 126)
        {
            InDb = Ar.Read<byte>();
        }
    }
}
