using System;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.FileProvider
{
    public class StreamedFileProvider : AbstractVfsFileProvider
    {
        public string LiveGame { get; }

        [Obsolete("Use the other constructors with explicit StringComparer")]
        public StreamedFileProvider(string liveGame, bool isCaseInsensitive = false, VersionContainer? versions = null)
            : this(liveGame, versions, isCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal) { }
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
