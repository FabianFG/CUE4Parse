using System.Collections.Generic;
using Serilog;

namespace CUE4Parse.FileProvider
{
    public abstract class AbstractFileProvider : IFileProvider
    {
        protected static ILogger log = Log.ForContext<IFileProvider>();
        
        public abstract IReadOnlyDictionary<string, GameFile> Files { get; }

        public GameFile this[string path] => Files[path];

        public bool TryFindGameFile(string path, out GameFile file)
        {
            return Files.TryGetValue(path, out file);
        }
    }
}