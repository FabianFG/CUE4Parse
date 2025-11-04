using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.StructUtils;

/// <summary>
/// Custom struct to serialize fixed size structs
/// </summary>
public class FFixedSizeStruct : IUStruct
{
    public byte[] Data = [];

    public FFixedSizeStruct(FAssetArchive Ar, int len = 0)
    {
        Data = len > 0 ? Ar.ReadBytes(len) : [];
    }
}

public struct FRawStruct<T> : IUStruct
{
    public T Value;
}
