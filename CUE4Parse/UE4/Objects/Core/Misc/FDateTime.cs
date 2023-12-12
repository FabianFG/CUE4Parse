using System;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse.UE4.Objects.Core.Misc;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FDateTime : IUStruct, ISerializable
{
    public readonly long Ticks;

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Write(Ticks);
    }
    
    public override string ToString() => $"{new DateTime(Ticks):F}";
}