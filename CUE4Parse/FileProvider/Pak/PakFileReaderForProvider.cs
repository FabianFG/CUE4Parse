using System.Collections.Concurrent;
using System.Collections.Generic;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;

namespace CUE4Parse.UE4.Pak
{
    public partial class PakFileReader
    {
        public void MountTo(FAesKey key, ConcurrentDictionary<string, ConcurrentDictionary<string, GameFile>> outFiles)
        {
            AesKey = key;
            ReadIndex(outFiles);
        }
    }
}