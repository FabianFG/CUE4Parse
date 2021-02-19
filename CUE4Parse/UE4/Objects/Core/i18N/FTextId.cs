using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Core.i18N
{
    public class FTextId : IUClass
    {
        public readonly FTextKey Namespace;
        public readonly FTextKey Key;

        public FTextId(FArchive Ar)
        {
            Namespace = new FTextKey(Ar);
            Key = new FTextKey(Ar);
        }

        public FTextId(FTextKey namespce, FTextKey key)
        {
            Namespace = namespce;
            Key = key;
        }
    }
}