using System.Collections.Generic;

namespace CUE4Parse.FileProvider
{
    public interface IFileProvider
    {
        public IReadOnlyDictionary<string, GameFile> Files { get; }

        public GameFile this[string path] { get; }
        public bool TryFindGameFile(string path, out GameFile file);
    }
}