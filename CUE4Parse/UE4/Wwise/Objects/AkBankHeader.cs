using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
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
    public readonly EAltValues AltValues;
    public readonly uint ProjectId;
    public readonly EAkBankTypeEnum SoundBankType;
    public readonly byte[] BankHash;

    // CAkBankMgr::ProcessBankHeader
    public AkBankHeader(FArchive Ar, int sectionLength)
    {
        Version = Ar.Read<uint>(); // If version is less than 26 there's two params before this read, support for versions < 100 isn't needed anyway
        SoundBankId = Ar.Read<uint>();
        LanguageId = Ar.Read<uint>();

        FeedbackInBank = false;
        AltValues = 0;
        ProjectId = 0;
        SoundBankType = 0;
        BankHash = [];

        if (Version <= 26)
        {
            Ar.Read<ulong>(); // timestamp
        }
        else if (Version <= 126)
        {
            FeedbackInBank = (Ar.Read<uint>() & 1) != 0;
        }
        else if (Version <= 134)
        {
            AltValues = Ar.Read<EAltValues>();
        }
        else
        {
            AltValues = Ar.Read<EAltValues>();
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
