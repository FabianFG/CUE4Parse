using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CUE4Parse.FileProvider.Pak;
using CUE4Parse.UE4.Pak;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;

namespace CUE4Parse.FileProvider
{
    public class FileProvider : AbstractPakFileProvider
    {

        public UE4Version Ver { get; set; } = UE4Version.VER_UE4_LATEST;
        public EGame Game { get; set; } = EGame.GAME_UE4_LATEST;

        public FileProvider(DirectoryInfo dir, bool isCaseInsensitive = false) : base(isCaseInsensitive)
        {
            if (!dir.Exists)
                throw new ArgumentException("Given Directory must exist", nameof(dir));
            ScanGameDirectory(dir, true);
            
        }

        private void ScanGameDirectory(DirectoryInfo dir, bool recurse)
        {
            // Container for files located in the os file system
            var osFiles = new Dictionary<string, GameFile>();
            
            // Iterate over all directories recursively
            foreach (var file in dir.EnumerateFiles("*.*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
            {
                var ext = file.Extension.SubstringAfter('.');
                if (string.IsNullOrEmpty(ext) || !file.Exists) return;
                
                // If we got a pak file, add it to the list
                if (ext.Equals("pak", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var reader = new PakFileReader(file, Ver, Game) {IsConcurrent = true};
                        _unloadedPaks[reader] = null; 
                        if (reader.IsEncrypted && !_requiredKeys.ContainsKey(reader.Info.EncryptionKeyGuid))
                            _requiredKeys[reader.Info.EncryptionKeyGuid] = null;
                    }
                    catch (Exception e)
                    {
                        log.Warning(e.ToString());
                    }
                }
                else
                {
                    // Register local file only if it has a known extension, we don't need every file
                    if (GameFile.Ue4KnownExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                    {
                        var osFile = new OsGameFile(dir, file);
                        var path = osFile.Path;
                        if (IsCaseInsensitive)
                            osFiles[path.ToLowerInvariant()] = osFile;
                        else
                            osFiles[path] = osFile;
                    }
                }
            }

            if (osFiles.Count > 0)
            {
                _files.AddFiles(osFiles);
            }
        }
    }
}