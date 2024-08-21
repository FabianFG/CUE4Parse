using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.StructUtils;

public struct FInstancedStructContainer : IUStruct
{
    public FStructFallback[]? Structs;

    public FInstancedStructContainer(FAssetArchive Ar)
    {
        var version = Ar.Read<EVersion>();

        FPackageIndex[] Items = Ar.ReadArray(() => new FPackageIndex(Ar));
        Structs = new FStructFallback[Items.Length];
        for (int index = 0; index < Items.Length; index++)
        {
            var item = Items[index];
            var size = Ar.Read<int>();
            var savedPos = Ar.Position;
            FStructFallback? strukt = null;
            if (item.TryLoad<UStruct>(out var NonConstStruct))
            {
                try
                {
                    strukt = new FStructFallback(Ar, NonConstStruct);
                }
                catch
                {
                    Ar.Position = savedPos + size;
                }

                Structs[index] = strukt;
            }
            else
            {
                Ar.Position += size;
            }
        }
    }

    enum EVersion : byte
    {
        InitialVersion = 0,
        // -----<new versions can be added above this line>-----
        VersionPlusOne,
        LatestVersion = VersionPlusOne - 1
    }
}
