using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.Engine.Material;

public struct FMaterialLayersFunctionsTree : IUStruct
{
    public FNode[] Nodes;
    public FPayload[] Payloads;
    public int Root = -1;

    public FMaterialLayersFunctionsTree(FAssetArchive Ar)
    {
        Nodes = Ar.ReadArray<FNode>();
        Payloads = Ar.ReadArray<FPayload>();
        Root = Ar.Read<int>();
    }

    private const int InvalidId = -1; // Invalid id is always <0

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FNode
    {
        public readonly int	Parent = InvalidId;
        public readonly int	NextSibling = InvalidId;
        public readonly int	ChildrenHead = InvalidId;
        public readonly int	Spare = InvalidId;

        public FNode() { }
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FPayload
    {
        public readonly int Layer = InvalidId;
        public readonly int Blend = InvalidId;

        public FPayload() { }
    }
}
