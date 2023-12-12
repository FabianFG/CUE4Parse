using System.Runtime.InteropServices;
using CUE4Parse.UE4;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse.GameTypes.TSW.Objects;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FSpeedQuantity : IUStruct, ISerializable
{
    public readonly float Value;

    public FSpeedQuantity(float InValue)
    {
        Value = InValue;
    }

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Write(Value);
    }
}