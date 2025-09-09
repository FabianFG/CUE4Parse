using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Core.i18N;

public class FTextLocalizationResourceString : IUStruct
{
    public readonly string String;
    public int RefCount;

    public FTextLocalizationResourceString(FArchive Ar, ELocResVersion versionNumber)
    {
        String = Ar.ReadFString();
        RefCount = versionNumber >= ELocResVersion.Optimized_CRC32 ? Ar.Read<int>() : -1;
    }

    public FTextLocalizationResourceString(string s, int refCount)
    {
        String = s;
        RefCount = refCount;
    }
}
