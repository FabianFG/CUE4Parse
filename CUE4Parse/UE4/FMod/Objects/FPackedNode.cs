using System.IO;

namespace CUE4Parse.UE4.FMod.Objects;

public partial class FRadixTreePacked
{
    public readonly struct FPackedNode
    {
        public readonly uint KeyInfo;
        public readonly uint ChildInfo;

        public FPackedNode(BinaryReader Ar)
        {
            KeyInfo = Ar.ReadUInt32();
            ChildInfo = Ar.ReadUInt32();
        }

        public readonly int StringOffset => (int) (KeyInfo & 0x00FFFFFF);
        public readonly bool HasString => StringOffset != Sentinel24;
    }
}
