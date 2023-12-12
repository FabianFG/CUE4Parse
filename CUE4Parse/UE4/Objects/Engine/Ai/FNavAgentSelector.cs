using System.Runtime.InteropServices;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse.UE4.Objects.Engine.Ai;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FNavAgentSelector : IUStruct, ISerializable
{
    public readonly uint PackedBits;

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Write(PackedBits);
    }
}