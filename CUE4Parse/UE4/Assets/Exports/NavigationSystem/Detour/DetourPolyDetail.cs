using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Assets.Exports.NavigationSystem.Detour;

[StructLayout(LayoutKind.Sequential)]
public struct DetourPolyDetail
{
    public ushort VertBase;
    public ushort TriBase;
    public byte VertCount;
    public byte TriCount;
}