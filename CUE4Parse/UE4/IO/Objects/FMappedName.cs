using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.IO.Objects
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FMappedName
    {
        private const int IndexBits = 30;
        private const uint IndexMask = (1u << IndexBits) - 1u;
        private const uint TypeMask = ~IndexMask;
        private const int TypeShift = IndexBits;
        
        private readonly uint _nameIndex;
        public readonly uint ExtraIndex;
        
        public uint NameIndex => _nameIndex & IndexMask;
        public EType Type => (EType) ((_nameIndex & TypeMask) >> TypeShift);
        public bool IsGlobal => ((_nameIndex & TypeMask) >> TypeShift) != 0;

        public enum EType
        {
            Package,
            Container,
            Global
        }
    }
}