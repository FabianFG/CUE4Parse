using System.IO;

namespace CUE4Parse.UE4.FMod.Nodes;

public class SoundDataHeaderNode
{
    public readonly FSoundDataHeader[] Header;

    public SoundDataHeaderNode(BinaryReader Ar)
    {
        Header = FModReader.ReadElemListImp<FSoundDataHeader>(Ar);
    }

    public readonly struct FSoundDataHeader
    {
        public readonly int FSBOffset;
        public readonly int IdkOffset;

        public FSoundDataHeader(BinaryReader Ar)
        {
            FSBOffset = Ar.ReadInt32();
            IdkOffset = Ar.ReadInt32();
        }
    }
}
