using System.Runtime.InteropServices;
using CUE4Parse.UE4.Wwise.Enums;
using CUE4Parse.UE4.Wwise.Enums.Flags;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects;

[JsonConverter(typeof(BankHeaderConverter))]
[StructLayout(LayoutKind.Sequential)]
public readonly struct AkBankHeader
{
    public readonly uint Version;
    public readonly uint SoundBankId;
    public readonly uint LanguageId;
    public readonly bool FeedbackInBank;
    public readonly EAltValuesFlags AltValues;
    public readonly uint ProjectId;
    public readonly EAkBankTypeEnum SoundBankType;
    public readonly byte[] BankHash = [];

    // CAkBankMgr::ProcessBankHeader
    public AkBankHeader(FWwiseArchive Ar, int sectionLength)
    {
        Version = Ar.Read<uint>(); // If version is less than 26 there's two params before this read
        SoundBankId = Ar.Read<uint>();
        LanguageId = Ar.Read<uint>();

        switch (Version)
        {
            case <= 26:
                Ar.Read<ulong>(); // timestamp
                break;
            case <= 126:
                FeedbackInBank = (Ar.Read<uint>() & 1) != 0;
                break;
            case <= 134:
                AltValues = Ar.Read<EAltValuesFlags>();
                break;
            default:
                AltValues = Ar.Read<EAltValuesFlags>();
                break;
        }

        if (Version > 76)
        {
            ProjectId = Ar.Read<uint>();
        }

        if (Version > 141)
        {
            SoundBankType = Ar.Read<EAkBankTypeEnum>();
            BankHash = Ar.ReadBytes(0x10);
        }

        // Determine padding size
        int gapSize = Version switch
        {
            <= 26 => (sectionLength - 0x18),
            <= 76 => (sectionLength - 0x10),
            <= 141 => (sectionLength - 0x14),
            _ => (sectionLength - 0x14 - 0x04 - 0x10)
        };

        if (gapSize > 0)
            Ar.Position += gapSize;
    }
}

public readonly struct FAKPKHeader(FWwiseArchive Ar)
{
    public readonly bool Endianness = Ar.ReadBoolean();
    public readonly uint NamesSectionLength = Ar.Read<uint>();
    public readonly uint BanksSectionLength = Ar.Read<uint>();
    public readonly uint SoundsSectionLength = Ar.Read<uint>();
    public readonly uint ExternalSoundsSectionLength = Ar.Read<uint>();

    public static long NamesOffset => 28; // sectionHeader + sizeof(FAKPKHeader)
    public readonly long BanksOffset => NamesOffset + NamesSectionLength;
    public readonly long WemsOffset => BanksOffset + BanksSectionLength;
    public readonly long ExternalWemsOffset => WemsOffset + SoundsSectionLength;
}
