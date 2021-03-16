using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.FileProvider
{
    public class StreamedFileProvider : AbstractVfsFileProvider
    {
        public StreamedFileProvider(bool caseSensitive = false,
            EGame game = EGame.GAME_UE4_LATEST, UE4Version ver = UE4Version.VER_UE4_DETERMINE_BY_GAME) : base(caseSensitive, game, ver)
        {
            
        }

        public override void Initialize()
        {
            
        }
    }
}