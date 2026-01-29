using System;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects;

[JsonConverter(typeof(AkEntryConverter))]
public class AkEntry
{
    public readonly uint NameHash;
    public readonly uint OffsetMultiplier;
    public readonly int Size;
    public readonly uint Offset;
    public readonly uint FolderId;
    public string? Path;
    public bool IsSoundBank => Data is { Length: >= 4 } && BitConverter.ToUInt32(Data) == (uint)EChunkID.BankHeader;
    public byte[]? Data;

    public AkEntry(FArchive Ar)
    {
        NameHash = Ar.Read<uint>();
        OffsetMultiplier = Ar.Read<uint>();
        Size = Ar.Read<int>();
        Offset = Ar.Read<uint>();
        FolderId = Ar.Read<uint>();
        // or should be Offset*OffsetMultiplier
        Data = Ar.ReadBytesAt(Offset, Size);
    }
}
