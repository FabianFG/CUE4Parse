using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects;

[JsonConverter(typeof(FInstancedStructConverter))]
public class FInstancedStruct : IUStruct
{
    public readonly FStructFallback? NonConstStruct;

    public FInstancedStruct(FAssetArchive Ar)
    {
        if (FInstancedStructCustomVersion.Get(Ar) < FInstancedStructCustomVersion.Type.CustomVersionAdded)
        {
            var headerOffset = Ar.Position;
            var header = Ar.Read<uint>();

            const uint LegacyEditorHeader = 0xABABABAB;
            if (header != LegacyEditorHeader)
            {
                Ar.Position = headerOffset;
            }

            _ = Ar.Read<byte>(); // Old Version
        }

        var strucindex = new FPackageIndex(Ar);
        var serialSize = Ar.Read<int>();
        var savedPos = Ar.Position;
        if (strucindex.TryLoad<UStruct>(out var struc))
        {
            try
            {
                NonConstStruct = new FStructFallback(Ar, struc);
            }
            catch
            {
                Ar.Position = savedPos + serialSize;
            }
        }
        else
        {
            Ar.Position += serialSize;
        }
    }
}
