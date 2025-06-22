using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Animation.PoseSearch;

public class FSparsePoseMultiMap<T>(FAssetArchive Ar) where T : struct
{
    public T MaxKey = Ar.Read<T>();
    public T MaxValue = Ar.Read<T>();
    public T DeltaKeyValue = Ar.Read<T>();
    public T[] DataValues = Ar.ReadArray<T>();
}
