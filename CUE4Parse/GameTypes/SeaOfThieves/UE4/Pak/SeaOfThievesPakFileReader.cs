using System.Text;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Pak;

public partial class PakFileReader
{
    private void SeaOfThievesReadIndexLegacy(StringComparer pathComparer)
    {
        var key = AesKey ?? throw new InvalidAesKeyException("Sea of Thieves pak requires an AES key");

        using var index = new FByteArchive($"{Name} - Index", Ar.ReadBytesAt(Info.IndexOffset, (int) Info.IndexSize), Versions);

        index.Position += 2; // Probably encryption flag and key size

        var mountPoint = ReadSeaOfThievesString(index, key);
        var isRootMount = mountPoint == "../../../";
        ValidateMountPoint(ref mountPoint);
        if (mountPoint.Length == 0 && !isRootMount)
            throw new InvalidAesKeyException($"Given AES key '{AesKey?.KeyString}' did not decrypt '{Name}'");

        var fileCount = index.Read<int>();
        var files = new Dictionary<string, GameFile>(fileCount, pathComparer);
        for (var i = 0; i < fileCount; i++)
        {
            var entry = new FPakEntry(this, string.Empty, index)
            {
                Path = string.Concat(mountPoint, ReadSeaOfThievesString(index, key))
            };
            if (entry is { IsDeleted: true, Size: 0 }) continue;
            if (entry.IsEncrypted) EncryptedFileCount++;
            files[entry.Path] = entry;
        }

        MountPoint = mountPoint;
        Files = files;
        bDecrypted = true;
    }

    private static string ReadSeaOfThievesString(FArchive index, FAesKey key)
    {
        var length = index.Read<int>();
        var bytes = index.ReadBytes(index.Read<int>());
        bytes.DecryptInPlace(key);
        return Encoding.UTF8.GetString(bytes, 0, length);
    }
}
