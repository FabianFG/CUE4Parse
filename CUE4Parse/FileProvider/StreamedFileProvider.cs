using System;
using System.IO;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Pak;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;

namespace CUE4Parse.FileProvider
{
    public class StreamedFileProvider : AbstractVfsFileProvider
    {
        public string LiveGame { get; }
        
        public StreamedFileProvider(string liveGame, bool caseSensitive = false,
            EGame game = EGame.GAME_UE4_LATEST, UE4Version ver = UE4Version.VER_UE4_DETERMINE_BY_GAME) : base(caseSensitive, game, ver)
        {
            LiveGame = liveGame;
        }

        public void Initialize(string file = "", Stream[] stream = null!)
        {
            var ext = file.SubstringAfter('.');
            if (string.IsNullOrEmpty(ext)) return;
            
            if (ext.Equals("pak", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var reader = new PakFileReader(file, stream[0], Game, Ver) {IsConcurrent = true};
                    if (reader.IsEncrypted && !_requiredKeys.ContainsKey(reader.Info.EncryptionKeyGuid))
                    {
                        _requiredKeys[reader.Info.EncryptionKeyGuid] = null;
                    }
                    _unloadedVfs[reader] = null;
                }
                catch (Exception e)
                {
                    Log.Warning(e.ToString());
                }
            }
            else if (ext.Equals("utoc", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var reader = new IoStoreReader(file, stream[0], stream[1], EIoStoreTocReadOptions.ReadDirectoryIndex, Game, Ver) {IsConcurrent = true};
                    if (reader.IsEncrypted && !_requiredKeys.ContainsKey(reader.Info.EncryptionKeyGuid))
                    {
                        _requiredKeys[reader.Info.EncryptionKeyGuid] = null;
                    }
                    _unloadedVfs[reader] = null; 
                }
                catch (Exception e)
                {
                    Log.Warning(e.ToString());
                }
            }
        }
    }
}