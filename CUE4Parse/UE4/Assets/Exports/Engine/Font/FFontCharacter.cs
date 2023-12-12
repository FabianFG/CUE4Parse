using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse.UE4.Assets.Exports.Engine.Font;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FFontCharacter : IUStruct, ISerializable
{
    public readonly int StartU;
    public readonly int StartV;
    public readonly int USize;
    public readonly int VSize;
    public readonly byte TextureIndex;
    public readonly int VerticalOffset;

    public FFontCharacter(FArchive Ar)
    {
        StartU = Ar.Read<int>();
        StartV = Ar.Read<int>();
        USize = Ar.Read<int>();
        VSize = Ar.Read<int>();
        TextureIndex = Ar.Read<byte>();
        VerticalOffset = Ar.Read<int>();
    }

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Write(StartU);
        Ar.Write(StartV);
        Ar.Write(USize);
        Ar.Write(VSize);
        Ar.Write(TextureIndex);
        Ar.Write(VerticalOffset);
    }

    public FFontCharacter(int startU, int startV, int uSize, int vSize, byte textureIndex, int verticalOffset)
    {
        StartU = startU;
        StartV = startV;
        USize = uSize;
        VSize = vSize;
        TextureIndex = textureIndex;
        VerticalOffset = verticalOffset;
    }
}