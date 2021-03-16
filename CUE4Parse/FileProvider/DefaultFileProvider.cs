using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Pak;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;

namespace CUE4Parse.FileProvider
{
    public class DefaultFileProvider : AbstractVfsFileProvider
    {
        private DirectoryInfo _workingDirectory;
        private SearchOption _searchOption;
        
        public DefaultFileProvider(string directory, SearchOption searchOption, bool caseSensitive = false,
            EGame game = EGame.GAME_UE4_LATEST, UE4Version ver = UE4Version.VER_UE4_DETERMINE_BY_GAME) :
            this(new DirectoryInfo(directory), searchOption, caseSensitive, game, ver) {}
        public DefaultFileProvider(DirectoryInfo directory, SearchOption searchOption, bool caseSensitive = false,
            EGame game = EGame.GAME_UE4_LATEST, UE4Version ver = UE4Version.VER_UE4_DETERMINE_BY_GAME) : base(caseSensitive, game, ver)
        {
            _workingDirectory = directory;
            _searchOption = searchOption;
        }

        /// <summary>
        /// Scan given <see cref="DirectoryInfo"/> for packages
        /// </summary>
        /// <exception cref="ArgumentException">Directory doesn't exist</exception>
        public override void Initialize()
        {
            if (!_workingDirectory.Exists) throw new ArgumentException("Given directory must exist", nameof(_workingDirectory));
            
            var osFiles = new Dictionary<string, GameFile>();
            foreach (var file in _workingDirectory.EnumerateFiles("*.*", _searchOption))
            {
                var ext = file.Extension.SubstringAfter('.');
                if (!file.Exists || string.IsNullOrEmpty(ext)) return;
                
                if (ext.Equals("pak", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var reader = new PakFileReader(file, Game, Ver) {IsConcurrent = true};
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
                        var reader = new IoStoreReader(file, EIoStoreTocReadOptions.ReadDirectoryIndex, Game, Ver) {IsConcurrent = true};
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
                else
                {
                    // Register local file only if it has a known extension, we don't need every file
                    if (!GameFile.Ue4KnownExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase)) continue;
                    
                    var osFile = new OsGameFile(_workingDirectory, file, Game, Ver);
                    if (IsCaseInsensitive) osFiles[osFile.Path.ToLowerInvariant()] = osFile;
                    else osFiles[osFile.Path] = osFile;
                }
            }
            
            _files.AddFiles(osFiles);
        }
    }
}