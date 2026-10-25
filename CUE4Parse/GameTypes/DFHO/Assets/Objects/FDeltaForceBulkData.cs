using System.Diagnostics.CodeAnalysis;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.GameTypes.DFHO.Assets.Objects;

public static class FDeltaForceBulkData
{
    public static bool IsUHD(FByteBulkDataHeader header) =>
        (header.BulkDataFlags & (EBulkDataFlags) 0x80000) != 0;

    public static bool TryGetArchive(FAssetArchive Ar, FByteBulkDataHeader header, [NotNullWhen(true)] out FAssetArchive? archive, out long position)
    {
        archive = null;
        position = 0;
        if (Ar.Owner?.Provider is not IVfsFileProvider provider) return false;

        var extension = header.CookedIndex.IsDefault ? ".uhd" : $"{header.CookedIndex}.uhd";
        var path = Path.ChangeExtension(Ar.Name, extension);
        if (!provider.TryGetGameFile(path, out var file) || !file.TryCreateReader(out var reader, header))
            return false;

        archive = new FAssetArchive(reader, Ar.Owner);
        position = archive.Length == header.SizeOnDisk ? 0 : header.OffsetInFile;
        return true;
    }
}
