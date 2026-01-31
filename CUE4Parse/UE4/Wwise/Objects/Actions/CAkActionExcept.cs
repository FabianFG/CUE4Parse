using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class CAkActionExcept
{
    public readonly WwiseObjectIDext[] ExceptionElements;

    // CAkActionExcept::SetExceptParams
    public CAkActionExcept(FArchive Ar)
    {
        int exceptionListSize;
        if (WwiseVersions.Version <= 122)
        {
            exceptionListSize = (byte)Ar.Read<uint>();
        }
        else
        {
            exceptionListSize = WwiseReader.Read7BitEncodedIntBE(Ar);
        }

        ExceptionElements = Ar.ReadArray(exceptionListSize, () => new WwiseObjectIDext(Ar));
    }
}

public readonly struct WwiseObjectIDext
{
    public readonly uint Id;
    public readonly bool IsBus;

    public WwiseObjectIDext(FArchive Ar)
    {
        Id = Ar.Read<uint>();

        if (WwiseVersions.Version > 65)
        {
            IsBus = Ar.Read<byte>() is not 0;
        }
    }
}
