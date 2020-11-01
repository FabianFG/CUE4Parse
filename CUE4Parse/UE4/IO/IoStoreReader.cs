using System.Collections.Generic;
using System.IO;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Vfs;
using Serilog;

namespace CUE4Parse.UE4.IO
{
    public class IoStoreReader : VirtualFileSystemReader
    {
        private static readonly ILogger log = Log.ForContext<IoStoreReader>();

        public readonly FArchive Ar;

        public IoStoreReader(FArchive containerStream, FArchive tocStream) : 
            base(containerStream.Name, containerStream.Ver, containerStream.Game)
        {
            
        }
        
        public override byte[] Extract(VfsEntry entry)
        {
            throw new System.NotImplementedException();
        }

        public override IReadOnlyDictionary<string, GameFile> Mount(bool caseInsensitive = false)
        {
            throw new System.NotImplementedException();
        }
    }
}