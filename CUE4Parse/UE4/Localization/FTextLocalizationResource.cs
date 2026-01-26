using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.HighPerformance;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Localization;

[JsonConverter(typeof(FTextLocalizationResourceConverter))]
public class FTextLocalizationResource
{
    private readonly FGuid _locResMagic = new (0x7574140Eu, 0xFC034A67u, 0x9D90154Au, 0x1B7F37C3u);
    public readonly Dictionary<FTextKey, Dictionary<FTextKey, FEntry>> Entries = [];

        public FTextLocalizationResource(FArchive Ar)
        {
            var locResMagic = Ar.Read<FGuid>();
            var versionNumber = ELocResVersion.Legacy;
            if (locResMagic == _locResMagic)
            {
                versionNumber = Ar.Read<ELocResVersion>();
            }
            else // Legacy LocRes files lack the magic number, assume that's what we're dealing with, and seek back to the start of the file
            {
                Ar.Position = 0;
                Log.Warning($"LocRes '{Ar.Name}' failed the magic number check! Assuming this is a legacy resource");
            }

        // Is this LocRes file too new to load?
        if (versionNumber > ELocResVersion.Latest)
        {
            if (Ar.Game != EGame.GAME_StellarBlade)
                throw new ParserException(Ar, $"LocRes '{Ar.Name}' is too new to be loaded (File Version: {versionNumber:D}, Loader Version: {ELocResVersion.Latest:D})");
        }

        // Read the localized string array
        var localizedStringArray = Array.Empty<FTextLocalizationResourceString>();
        if (versionNumber >= ELocResVersion.Compact)
        {
            if (Ar.Game == EGame.GAME_NevernessToEverness && Ar.Name.StartsWith("HT/Content/Localization/"))
                Ar.Position += 4;
            var localizedStringArrayOffset = Ar.Read<long>();
            if (localizedStringArrayOffset != -1) // INDEX_NONE
            {
                var currentFileOffset = Ar.Position;
                Ar.Position = localizedStringArrayOffset;
                if (Ar.Game is EGame.GAME_CodeVein2 && Ar.Name.Contains("CodeVein2/Content/Localization/"))
                {
                    localizedStringArray = ReadCodeVein2LocalizationResourceStrings(Ar, versionNumber);
                }
                else
                    localizedStringArray = Ar.ReadArray(() => new FTextLocalizationResourceString(Ar, versionNumber));
                Ar.Position = currentFileOffset;
            }
        }

        // Read entries count
        if (versionNumber >= ELocResVersion.Optimized_CRC32)
        {
            Ar.Position += 4; // EntriesCount
        }

        // Read namespace count
        var namespaceCount = Ar.Read<uint>();
        for (var i = 0; i < namespaceCount; i++)
        {
            var namespce = new FTextKey(Ar, versionNumber);
            var keyCount = Ar.Read<uint>();
            var keyValue = new Dictionary<FTextKey, FEntry>((int)keyCount);
            for (var j = 0; j < keyCount; j++)
            {
                var key = new FTextKey(Ar, versionNumber);
                FEntry newEntry = new(Ar);
                if (versionNumber >= ELocResVersion.Compact)
                {
                    var localizedStringIndex = Ar.Read<int>();
                    if (localizedStringArray.Length > localizedStringIndex)
                    {
                        // Steal the string if possible
                        var localizedString = localizedStringArray[localizedStringIndex];
                        newEntry.LocalizedString = localizedString.String;
                        if (localizedString.RefCount != -1) localizedString.RefCount--;
                    }
                    else
                    {
                        Log.Warning($"LocRes '{newEntry.LocResName}' has an invalid localized string index for namespace '{namespce.Str}' and key '{key.Str}'. This entry will have no translation.");
                    }

                    if (Ar.Game == EGame.GAME_StellarBlade && versionNumber > ELocResVersion.Latest) Ar.Position += 4;
                }
                else
                {
                    newEntry.LocalizedString = Ar.ReadFString();
                }

                keyValue.Add(key, newEntry);
            }
            Entries.Add(namespce, keyValue);
        }
    }

    private static readonly byte[] CodeVein2Xorkey =
    {
        0x6D, 0xC0, 0xE5, 0x02, 0x17, 0x55, 0x29, 0xF2, 0x0E, 0x1F, 0x68, 0x0D, 0xAD, 0x3E, 0xF8, 0x2C,
        0x5F, 0x9E, 0xC2, 0x20, 0xEB, 0x54, 0xBE, 0x2E, 0x23, 0xA1, 0xA4, 0x7A, 0xE3, 0x09, 0x4C, 0x51,
        0xFD, 0x9B, 0x6E, 0xF9, 0x8B, 0x00, 0x37, 0xD4, 0x74, 0xA2, 0x64, 0xA0, 0xC3, 0x5C, 0x36, 0xE6,
        0x15, 0x0B, 0x1C, 0xFE, 0x3C, 0xAB, 0xF1, 0xE4, 0xC7, 0xAE, 0x3D, 0xB9, 0x01, 0x76, 0xAA, 0x21
    };

    private FTextLocalizationResourceString[] ReadCodeVein2LocalizationResourceStrings(FArchive Ar, ELocResVersion versionNumber)
    {
        FTextLocalizationResourceString[] localizedStringArray;
        var stringCount = Ar.Read<int>();
        localizedStringArray = new FTextLocalizationResourceString[stringCount];
        for (var i = 0; i < stringCount; i++)
        {
            var length = Ar.Read<int>();
            if (length >= 0)
            {
                var str = Ar.ReadArray<byte>(length).AsSpan();
                var refcount = versionNumber >= ELocResVersion.Optimized_CRC32 ? Ar.Read<int>() : -1;
                length--;

                for (var c = 0; c < length; c++)
                {
                    var xorChar = CodeVein2Xorkey[(length + c) & 0x3f];
                    str[c] ^= xorChar;
                }
                localizedStringArray[i] = new FTextLocalizationResourceString(Encoding.UTF8.GetString(str[..^1]), refcount);
            }
            else
            {
                length = -length;
                var str = Ar.ReadArray<char>(length).AsSpan();
                var refcount = versionNumber >= ELocResVersion.Optimized_CRC32 ? Ar.Read<int>() : -1;
                length--;

                for (var c = 0; c < length; c++)
                {
                    var xorChar = (char) (CodeVein2Xorkey[(length + c) & 0x3f]);
                    if (str[c] == 0xf000) str[c] = (char) 0x00;
                    if (str[c] == 0xf001) str[c] = (char) 0xffff;
                    str[c] ^= xorChar;
                }
                localizedStringArray[i] = new FTextLocalizationResourceString(Encoding.Unicode.GetString(str[..^1].AsBytes()), refcount);
            }
        }

        return localizedStringArray;
    }
}
