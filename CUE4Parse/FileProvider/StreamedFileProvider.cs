using System;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.FileProvider
{
    public class StreamedFileProvider : AbstractVfsFileProvider
    {
        public string LiveGame { get; }

        public StreamedFileProvider(string liveGame, VersionContainer? versions = null, StringComparer? pathComparer = null) : base(versions, pathComparer)
        {
            LiveGame = liveGame;
        }

        public override void Initialize()
        {
            // there should be nothing here ig
        }
    }
}
