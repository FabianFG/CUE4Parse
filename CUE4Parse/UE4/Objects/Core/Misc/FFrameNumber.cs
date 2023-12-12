using System.Runtime.InteropServices;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse.UE4.Objects.Core.Misc;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FFrameNumber : IUStruct, ISerializable
{
    public readonly int Value;

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Write(Value);
    }

    public override string ToString() => Value.ToString();
}