using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class ExceptParams
{
    public byte ExceptionListSize { get; private set; }
    public List<ExceptionElement> ExceptionElements { get; private set; }

    public ExceptParams(FArchive Ar)
    {
        if (WwiseVersions.WwiseVersion <= 122)
        {
            ExceptionListSize = (byte)Ar.Read<uint>();
        }
        else
        {
            ExceptionListSize = Ar.Read<byte>();
        }

        ExceptionElements = [];
        for (int i = 0; i < ExceptionListSize; i++)
        {
            ExceptionElements.Add(new ExceptionElement(Ar));
        }
    }
}

public class ExceptionElement
{
    public uint Id { get; private set; }
    public byte? IsBus { get; private set; }

    public ExceptionElement(FArchive Ar)
    {
        Id = Ar.Read<uint>();

        if (WwiseVersions.WwiseVersion > 65)
        {
            IsBus = Ar.Read<byte>();
        }
    }
}
