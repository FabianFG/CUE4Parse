using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects.OnDemand.V1;

public class FOnDemandTocAdditionalFile
{
    public FSHAHash Hash;
    public string FileName;
    public ulong FileSize;
    
    public FOnDemandTocAdditionalFile(FArchive Ar)
    {
        Hash = new FSHAHash(Ar);
        FileName = Ar.ReadFString();
        FileSize = Ar.Read<ulong>();
    }
}