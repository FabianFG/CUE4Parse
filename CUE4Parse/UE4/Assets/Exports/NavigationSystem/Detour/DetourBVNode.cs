using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.NavigationSystem.Detour;

public struct DetourBVNode
{
    public ushort[] BMin;
    public ushort[] BMax;
    public int Index;
    
    public DetourBVNode(FArchive Ar)
    {
        BMin = Ar.ReadArray<ushort>(3);
        BMax = Ar.ReadArray<ushort>(3);
        Index = Ar.Read<int>();
    }
}