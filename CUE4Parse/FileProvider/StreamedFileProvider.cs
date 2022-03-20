using System;
using System.IO;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Pak;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;

namespace CUE4Parse.FileProvider
{
    public class StreamedFileProvider : AbstractVfsFileProvider
    {
        public string LiveGame { get; }

        public StreamedFileProvider(string liveGame, bool isCaseInsensitive = false, VersionContainer? versions = null) : base(isCaseInsensitive, versions)
        {
            LiveGame = liveGame;
        }

        public void Initialize(string file = "", Stream[] stream = null!, Func<string, FArchive>? openContainerStreamFunc = null)
        {
            var ext = file.SubstringAfter('.');
            if (string.IsNullOrEmpty(ext)) return;

            if (ext.Equals("pak", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var reader = new PakFileReader(file, stream[0], Versions) {IsConcurrent = true, CustomEncryption = CustomEncryption};
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
                openContainerStreamFunc ??= it => new FStreamArchive(it, stream[1], Versions);

                try
                {
                    var reader = new IoStoreReader(file, stream[0], openContainerStreamFunc, EIoStoreTocReadOptions.ReadDirectoryIndex, Versions) {IsConcurrent = true, CustomEncryption = CustomEncryption};
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
