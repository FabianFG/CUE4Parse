using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Pak;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using Ionic.Zip;

namespace CUE4Parse.FileProvider
{
    public class DefaultFileProvider : AbstractVfsFileProvider
    {
        private DirectoryInfo _workingDirectory;
        private readonly SearchOption _searchOption;
        private readonly List<DirectoryInfo> _extraDirectories;

        public DefaultFileProvider(string directory, SearchOption searchOption, bool isCaseInsensitive = false, VersionContainer? versions = null)
            : this(new DirectoryInfo(directory), searchOption, isCaseInsensitive, versions) { }

        public DefaultFileProvider(DirectoryInfo directory, SearchOption searchOption, bool isCaseInsensitive = false, VersionContainer? versions = null)
            : base(isCaseInsensitive, versions)
        {
            _workingDirectory = directory;
            _searchOption = searchOption;
        }

        public DefaultFileProvider(DirectoryInfo mainDirectory, List<DirectoryInfo> extraDirectories, SearchOption searchOption, bool isCaseInsensitive = false, VersionContainer? versions = null)
            : base(isCaseInsensitive, versions)
        {
            _workingDirectory = mainDirectory;
            _extraDirectories = extraDirectories;
            _searchOption = searchOption;
        }

        public void Initialize()
        {
            if (!_workingDirectory.Exists) throw new ArgumentException("Given directory must exist", nameof(_workingDirectory));

            var availableFiles = new List<Dictionary<string, GameFile>> {IterateFiles(_workingDirectory, _searchOption)};

            if (_extraDirectories is {Count: > 0})
            {
                availableFiles.AddRange(_extraDirectories.Select(directory => IterateFiles(directory, _searchOption)));
            }

            foreach (var osFiles in availableFiles)
            {
                _files.AddFiles(osFiles);
            }
        }

        private void RegisterFile(string file, Stream[] stream = null!, Func<string, FArchive>? openContainerStreamFunc = null)
        {
            var ext = file.SubstringAfterLast('.');
            if (ext.Equals("pak", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var reader = new PakFileReader(file, stream[0], Versions) { IsConcurrent = true, CustomEncryption = CustomEncryption };
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
                    var reader = new IoStoreReader(file, stream[0], openContainerStreamFunc, EIoStoreTocReadOptions.ReadDirectoryIndex, Versions) { IsConcurrent = true, CustomEncryption = CustomEncryption };
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

        private void RegisterFile(FileInfo file)
        {
            var ext = file.FullName.SubstringAfterLast('.');
            if (ext.Equals("pak", StringComparison.OrdinalIgnoreCase))
            {
                RegisterFile(file.FullName, new Stream[] { file.OpenRead() });
            }
            else if (ext.Equals("utoc", StringComparison.OrdinalIgnoreCase))
            {
                RegisterFile(file.FullName, new Stream[] { file.OpenRead() },
                    it => new FStreamArchive(it, File.OpenRead(it), Versions));
            }
            else if (ext.Equals("apk", StringComparison.OrdinalIgnoreCase))
            {
                var zipfile = new ZipFile(file.FullName);
                MemoryStream pngstream = new();
                foreach (var entry in zipfile.Entries)
                {
                    if (!entry.FileName.EndsWith("main.obb.png", StringComparison.OrdinalIgnoreCase))
                        continue;
                    entry.Extract(pngstream);
                    pngstream.Seek(0, SeekOrigin.Begin);

                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    var container = ZipFile.Read(pngstream);

                    foreach (var fileentry in container.Entries)
                    {
                        var streams = new Stream[2];
                        if (fileentry.FileName.EndsWith(".pak"))
                        {
                            try
                            {
                                streams[0] = new MemoryStream();
                                fileentry.Extract(streams[0]);
                                streams[0].Seek(0, SeekOrigin.Begin);
                            }
                            catch (Exception e)
                            {
                                Log.Warning(e.ToString());
                            }
                        }
                        else if (fileentry.FileName.EndsWith(".utoc"))
                        {
                            try
                            {
                                streams[0] = new MemoryStream();
                                fileentry.Extract(streams[0]);
                                streams[0].Seek(0, SeekOrigin.Begin);

                                foreach (var ucas in container.Entries) // look for ucas file
                                {
                                    if(ucas.FileName.Equals(fileentry.FileName.SubstringBeforeLast('.') + ".ucas"))
                                    {
                                        streams[1] = new MemoryStream();
                                        ucas.Extract(streams[1]);
                                        streams[1].Seek(0, SeekOrigin.Begin);
                                        break;
                                    }
                                }
                                if (streams[1] == null)
                                    continue; // ucas file not found
                            }
                            catch (Exception e)
                            {
                                Log.Warning(e.ToString());
                            }
                        }
                        else
                        {
                            continue;
                        }
                        RegisterFile(fileentry.FileName, streams);
                    }
                }
            }
        }

        private Dictionary<string, GameFile> IterateFiles(DirectoryInfo directory, SearchOption option)
        {
            var osFiles = new Dictionary<string, GameFile>();
            if (!directory.Exists) return osFiles;

            // Look for .uproject file to get the correct mount point
            var uproject = directory.GetFiles("*.uproject", SearchOption.TopDirectoryOnly).FirstOrDefault();
            string mountPoint;
            if (uproject != null)
            {
                mountPoint = uproject.Name.SubstringBeforeLast('.') + '/';
            }
            else
            {
                // Or use the directory name
                mountPoint = directory.Name + '/';
            }

            // In .uproject mode, we must recursively look for files
            option = uproject != null ? SearchOption.AllDirectories : option;

            foreach (var file in directory.EnumerateFiles("*.*", option))
            {
                var ext = file.Extension.SubstringAfter('.');
                if (!file.Exists || string.IsNullOrEmpty(ext)) continue;

                // Only load containers if .uproject file is not found
                if (uproject == null)
                {
                    RegisterFile(file);
                }

                // Register local file only if it has a known extension, we don't need every file
                if (!GameFile.Ue4KnownExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase)) continue;

                var osFile = new OsGameFile(_workingDirectory, file, mountPoint, Versions);
                if (IsCaseInsensitive) osFiles[osFile.Path.ToLowerInvariant()] = osFile;
                else osFiles[osFile.Path] = osFile;
            }

            return osFiles;
        }
    }
}
