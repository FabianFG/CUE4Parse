using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects;

[JsonConverter(typeof(AkEntryConverter))]
public class AkEntry
{
    public readonly uint NameHash;
    public readonly ulong ExternalNameHash;
    public readonly uint OffsetMultiplier;
    public readonly int Size;
    public readonly uint Offset;
    public readonly uint FolderId;
    public string AudioPath;
    public readonly bool IsSoundBank;
    public readonly bool IsExternalSound;
    public FDeferredByteData Data;

    public AkEntry(FWwiseArchive Ar, bool isSoundBank, bool externalSound = false)
    {
        IsSoundBank = isSoundBank;
        IsExternalSound = externalSound;
        if (externalSound)
        {
            ExternalNameHash = Ar.Read<ulong>();
        }
        else
        {
            NameHash = Ar.Read<uint>();
        }
        OffsetMultiplier = Ar.Read<uint>();
        Size = Ar.Read<int>();
        Offset = Ar.Read<uint>();
        FolderId = Ar.Read<uint>();
    }

    public string Name => IsExternalSound ? ExternalNameHash.ToString() : NameHash.ToString();

    public void ReadAudioPath(AkFolder[] folders) => AudioPath = Path.Combine(folders.FirstOrDefault(x => x.Id == FolderId)?.Name ?? "", Name + (IsSoundBank ? ".bnk" : ".wem"));

    public long ReadData(FWwiseArchive Ar, WwiseDataSource source)
    {
        Data = WwiseReader.ReadDeferredByteData(Ar, source, Offset * OffsetMultiplier, Size);
        return Data.LoadedSize;
    }
}
