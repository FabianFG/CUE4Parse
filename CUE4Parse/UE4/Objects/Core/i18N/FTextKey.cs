using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Core.i18N
{
    public class FTextKey : IUClass
    {
        public readonly string Str;
        public readonly uint StrHash;
        
        public FTextKey(FArchive Ar)
        {
            StrHash = Ar.Read<uint>();
            Str = Ar.ReadFString();
        }
    }
}