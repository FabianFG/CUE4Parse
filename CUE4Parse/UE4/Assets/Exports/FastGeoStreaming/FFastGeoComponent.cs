using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.FastGeoStreaming;

public class FFastGeoComponent(FArchive Ar)
{
    public int ComponentIndex = Ar.Read<int>();
}
