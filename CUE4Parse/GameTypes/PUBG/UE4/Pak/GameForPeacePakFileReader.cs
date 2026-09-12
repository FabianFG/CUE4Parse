using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Pak;

public partial class PakFileReader
{
    private static ReadOnlySpan<byte> GameForPeaceIniMagic => "Linimass"u8;
    private const ulong GameForPeaceIniMagicEncrypted = 0x4b4457585d5d5b7d;
    private static readonly byte[] _gameForPeaceIniDecryptTable =
    [
        0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
        0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x31,
        0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x31, 0x32,
        0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x31, 0x32, 0x33,
        0x35, 0x36, 0x37, 0x38, 0x39, 0x31, 0x32, 0x33, 0x34,
        0x36, 0x37, 0x38, 0x39, 0x31, 0x32, 0x33, 0x34, 0x35,
        0x37, 0x38, 0x39, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36,
        0x38, 0x39, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37,
        0x39, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
    ];

    private static byte[] DecryptGameForPeaceIni(byte[] data)
    {
        if (data.Length < sizeof(ulong))
            return data;

        if (BitConverter.ToUInt64(data, 0) == GameForPeaceIniMagicEncrypted)
        {
            TensorUtils.Xor(data, _gameForPeaceIniDecryptTable);
        }
        else if (!data.AsSpan().StartsWith(GameForPeaceIniMagic))
        {
            return data;
        }

        return data[sizeof(ulong)..];
    }

    private void GameForPeaceReadIndex(StringComparer pathComparer, FByteArchive index)
    {
        var saved = index.Position;
        var count = index.Read<int>();

        var oldVersion = false;
        try
        {
            var path = index.ReadFString();
        }
        catch
        {
            oldVersion = true;
        }
        finally
        {
            index.Position = saved;
        }

        if (!oldVersion)
        {
            var newentries = index.ReadMap(index.ReadFString, () => new FPakEntry(this, "", index, Game));
            var newfiles = new Dictionary<string, GameFile>(newentries.Count, pathComparer);
            foreach (var (key, value) in newentries)
            {
                var path = string.Concat(MountPoint, key);
                value.Path = path;
                newfiles[path] = value;
            }
            Files = newfiles;
            return;
        }

        var entries = index.ReadArray(() => new FPakEntry(this, "", index, Game));
        var files = new Dictionary<string, GameFile>(entries.Length, pathComparer);

        using var directoryIndex = new FByteArchive($"{Name} - Directory Index", ReadAndDecrypt((int) Ar.Read<long>()));

        var directoryIndexLength = (int) directoryIndex.Read<long>();
        for (var i = 0; i < directoryIndexLength; i++)
        {
            var dir = directoryIndex.ReadFString();
            var dirDictLength = (int) directoryIndex.Read<long>();

            for (var j = 0; j < dirDictLength; j++)
            {
                var name = directoryIndex.ReadFString();
                string path;
                if (MountPoint.EndsWith('/') && dir.StartsWith('/'))
                    path = dir.Length == 1 ? string.Concat(MountPoint, name) : string.Concat(MountPoint, dir[1..], name);
                else
                    path = string.Concat(MountPoint, dir, name);

                var indexf = directoryIndex.Read<int>();

                entries[indexf].Path = path;
                files[path] = entries[indexf];
            }
        }

        Files = files;
    }
}
