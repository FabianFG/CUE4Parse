using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.PCG;

public enum EPCGMetadataDomainFlag : byte
{
    /** Depends on the data. Should map to the same concept before multi-domain metadata. */
    Default = 0,

    /** Metadata at the data domain. */
    Data = 1,

    /** Metadata on elements like points on point data and entries on param data. */
    Elements = 2,

    /** For invalid domain. */
    Invalid = 254,

    /** For data that can have more domains. */
    Custom = 255
};

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 5)]
public struct FPCGMetadataDomainID
{
    public EPCGMetadataDomainFlag Flag = EPCGMetadataDomainFlag.Default;
    public int CustomFlag = -1;

    public FPCGMetadataDomainID(FAssetArchive Ar)
    {
        Flag = Ar.Read<EPCGMetadataDomainFlag>();
        CustomFlag = Ar.Read<int>();
    }

    public bool IsDefault() => Flag == EPCGMetadataDomainFlag.Default;
}
