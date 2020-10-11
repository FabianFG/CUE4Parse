using System;
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

        public FileProvider(DirectoryInfo dir)
        {
            if (!dir.Exists)
                throw new ArgumentException("Given Directory must exist", nameof(dir));
            ScanGameDirectory(dir, true);
            
        }

        private void ScanGameDirectory(DirectoryInfo dir, bool recurse)
        {
            foreach (var file in dir.EnumerateFiles("*.*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
            {
                RegisterGameFile(dir, file);
            }
        }

        private void RegisterGameFile(DirectoryInfo baseDir, FileInfo file)
        {
            var ext = file.Extension.SubstringAfter('.');
            if (string.IsNullOrEmpty(ext) || !file.Exists) return;

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
                RegisterOsFile(baseDir, file, ext);
            }
        }

        private void RegisterOsFile(DirectoryInfo baseDir, FileInfo file, string ext)
        {
            if (GameFile.Ue4KnownExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            {
                var osFile = new OsGameFile(baseDir, file);
                var path = osFile.Path;
                var dir = path.SubstringBeforeWithLast('/');
                var name = path.SubstringAfterLast('/');
                var dirDict = _files.GetOrAdd(dir.ToLower());
                dirDict[name] = osFile;
            }
        }
    }
}