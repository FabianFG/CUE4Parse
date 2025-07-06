using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.BinaryConfig.Objects;

public struct FConfigValue
{
    public string SavedValue;

    public FConfigValue(FArchive Ar)
    {
        SavedValue = Ar.ReadFString();
    }
}
