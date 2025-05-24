using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class ExceptParams
{
    public readonly List<ExceptionElement> ExceptionElements;

    public ExceptParams(FArchive Ar)
    {
        byte exceptionListSize;
        if (WwiseVersions.Version <= 122)
        {
            exceptionListSize = (byte)Ar.Read<uint>();
        }
        else
        {
            exceptionListSize = Ar.Read<byte>();
        }

        ExceptionElements = [];
        for (int i = 0; i < exceptionListSize; i++)
        {
            ExceptionElements.Add(new ExceptionElement(Ar));
        }
    }
}

public class ExceptionElement
{
    public readonly uint Id;
    public readonly byte? IsBus;

    public ExceptionElement(FArchive Ar)
    {
        Id = Ar.Read<uint>();

        if (WwiseVersions.Version > 65)
        {
            IsBus = Ar.Read<byte>();
        }
    }
}
