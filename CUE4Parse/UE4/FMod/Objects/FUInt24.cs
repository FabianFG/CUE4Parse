using System.IO;

namespace CUE4Parse.UE4.FMod.Objects;

public partial class FRadixTreePacked
{
    public readonly struct FUInt24
    {
        public readonly uint Value;

        public FUInt24(uint v) { Value = v; }

        public FUInt24(BinaryReader Ar)
        {
            Value = ReadUInt24(Ar);
        }
    }
}
