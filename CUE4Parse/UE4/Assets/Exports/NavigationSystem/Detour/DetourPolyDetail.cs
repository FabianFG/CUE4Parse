using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.NavigationSystem.Detour;

[StructLayout(LayoutKind.Sequential)]
public struct DetourPolyDetail
{
    public uint VertBase;
    public uint TriBase;
    public byte VertCount;
    public byte TriCount;

    public DetourPolyDetail(FArchive Ar)
    {
        if (Ar.Game >= Versions.EGame.GAME_UE5_0)
        {
            VertBase = Ar.Read<ushort>();
            TriBase = Ar.Read<ushort>();
        }
        else
        {
            VertBase = Ar.Read<uint>();
            TriBase = Ar.Read<uint>();
        }
        VertCount = Ar.Read<byte>();
        TriCount = Ar.Read<byte>();
    }
}
