namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class CAkActionExcept
{
    public readonly WwiseObjectIDext[] ExceptionElements;

    // CAkActionExcept::SetExceptParams
    public CAkActionExcept(FWwiseArchive Ar)
    {
        int exceptionListSize;
        if (Ar.Version <= 122)
        {
            exceptionListSize = (byte)Ar.Read<uint>();
        }
        else
        {
            exceptionListSize = Ar.Read7BitEncodedIntBE();
        }

        ExceptionElements = Ar.ReadArray(exceptionListSize, () => new WwiseObjectIDext(Ar));
    }
}

public readonly struct WwiseObjectIDext
{
    public readonly uint Id;
    public readonly bool IsBus;

    public WwiseObjectIDext(FWwiseArchive Ar)
    {
        Id = Ar.Read<uint>();

        if (Ar.Version > 65)
        {
            IsBus = Ar.Read<byte>() is not 0;
        }
    }
}
