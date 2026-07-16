using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.IO.Objects.OnDemand.V2;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FOnDemandStringEntry
{
    public readonly uint Offset;
    public readonly uint Length;

    public override string ToString() => $"Offset: {Offset} Length: {Length}";
}