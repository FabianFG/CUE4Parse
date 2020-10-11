using System.Collections.Generic;
using System.Threading.Tasks;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Pak;

namespace CUE4Parse.FileProvider.Pak
{
    public interface IPakFileProvider : IFileProvider
    {
        public IReadOnlyCollection<PakFileReader> UnloadedPaks { get; }
        public IReadOnlyCollection<PakFileReader> MountedPaks { get; }
        
        //Aes-Key Management
        public IReadOnlyDictionary<FGuid, FAesKey> Keys { get; }
        public IReadOnlyCollection<FGuid> RequiredKeys { get; }
        
        public int SubmitKey(FGuid guid, FAesKey key);
        public Task<int> SubmitKeyAsync(FGuid guid, FAesKey key);
        public int SubmitKeys(IEnumerable<KeyValuePair<FGuid, FAesKey>> keys);
        public Task<int> SubmitKeysAsync(IEnumerable<KeyValuePair<FGuid, FAesKey>> keys);
    }
}