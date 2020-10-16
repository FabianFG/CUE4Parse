using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider.Pak;

namespace CUE4Parse.UE4.Pak
{
    public partial class PakFileReader
    {
        public void MountTo(FAesKey key, FileProviderDictionary files, bool caseInsensitive)
        {
            AesKey = key;
            files.AddFiles(ReadIndex(caseInsensitive));
        }
    }
}