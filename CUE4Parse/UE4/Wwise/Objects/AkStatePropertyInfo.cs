using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkStatePropertyInfo
{
    public readonly byte PropertyId;
    public readonly byte AccumType;
    public readonly byte InDb;

    public AkStatePropertyInfo(FArchive Ar)
    {
        PropertyId = Ar.Read<byte>();
        AccumType = Ar.Read<byte>();
        if (WwiseVersions.Version > 126)
        {
            InDb = Ar.Read<byte>();
        }
    }
}
