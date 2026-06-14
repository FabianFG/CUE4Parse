using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise;

namespace CUE4Parse.GameTypes.RocoKingdomWorld.Assets.Objects;

public readonly struct FAudioEventEntry
{
    public readonly string Name;
    public readonly uint AudioEventId;
    public readonly string SubName;
    public readonly FAudioEventEntryData[] Data;
    public readonly bool Unknown;

    public FAudioEventEntry(FArchive Ar)
    {
        Name = Ar.ReadFString();
        AudioEventId = WwiseFnv.GetHash(Name);
        SubName = Ar.ReadFString();
        Data = Ar.ReadArray(() => new FAudioEventEntryData(Ar));
        Unknown = Ar.ReadBoolean();
    }
}

public readonly struct FAudioEventEntryData(FArchive Ar)
{
    public readonly string LanguageId = Ar.ReadFString();
    public readonly uint Value = Ar.Read<uint>();
}

public readonly struct FAudioEntry
{
    public readonly int Id;
    public readonly string Name;
    public readonly uint AudioEventId;
    public readonly string Description;
    public readonly byte Type;
    public readonly bool TypeDesc;

    public FAudioEntry(FArchive Ar, Dictionary<int, string> descTable, Dictionary<byte, bool> typeDescTable)
    {
        Id = Ar.Read<int>();
        Name = Ar.ReadFString();
        AudioEventId = WwiseFnv.GetHash(Name);
        Description = descTable.GetValueOrDefault(Id) ?? string.Empty;
        Type = Ar.Read<byte>();
        TypeDesc = typeDescTable.GetValueOrDefault(Type);
    }
}

public class FRocoAudioConfig
{
    public FAudioEntry[] Entries;
    public FAudioEventEntry[] EventEntries;
    public Dictionary<uint, string> SurfaceTypes;
    public Dictionary<uint, string> MaterialTypes;

    public FRocoAudioConfig(FArchive Ar, FArchive descAr, FArchive typeDescAr)
    {
        var typeDescTable = typeDescAr.ReadMap(typeDescAr.Read<byte>, () => typeDescAr.Read<byte>() != 0);
        var descTable = descAr.ReadMap(descAr.Read<int>, descAr.ReadFString);

        Entries = Ar.ReadArray(() => new FAudioEntry(Ar, descTable, typeDescTable));
        EventEntries = Ar.ReadArray(() => new FAudioEventEntry(Ar));
        SurfaceTypes = Ar.ReadMap(Ar.Read<uint>, Ar.ReadFString);
        MaterialTypes = Ar.ReadMap(Ar.Read<uint>, Ar.ReadFString);
    }
}
