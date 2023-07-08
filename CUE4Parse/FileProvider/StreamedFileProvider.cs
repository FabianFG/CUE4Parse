using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.FileProvider
{
    public class StreamedFileProvider : AbstractVfsFileProvider
    {
        public string LiveGame { get; }

        public StreamedFileProvider(string liveGame, bool isCaseInsensitive = false, VersionContainer? versions = null) : base(isCaseInsensitive, versions)
        {
            LiveGame = liveGame;
        }

        public override void Initialize()
        {
            // there should be nothing here ig
        }
    }
}
