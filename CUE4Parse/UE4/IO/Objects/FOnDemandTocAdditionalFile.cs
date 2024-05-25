using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects;

public class FOnDemandTocAdditionalFile
{
    public FSHAHash Hash;
    public string Filename;
    public ulong FileSize;
    
    public FOnDemandTocAdditionalFile(FArchive Ar)
    {
        Hash = Ar.Read<FSHAHash>();
        Filename = Ar.ReadFString();
        FileSize = Ar.Read<ulong>();
    }
}