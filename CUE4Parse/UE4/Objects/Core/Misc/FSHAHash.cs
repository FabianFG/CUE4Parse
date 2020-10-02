using CUE4Parse.UE4.Pak.Reader;

namespace CUE4Parse.UE4.Objects.Core.Misc
{
    public readonly struct FSHAHash : IUStruct
    {
        public readonly byte[] Hash;

        public FSHAHash(FPakArchive Ar)
        {
            Hash = Ar.ReadBytes(20);
        }
    }
}
