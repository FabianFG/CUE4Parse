using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Localization
{
    [JsonConverter(typeof(FTextLocalizationResourceConverter))]
    public class FTextLocalizationResource
    {
        private readonly FGuid _locResMagic = new (0x7574140Eu, 0xFC034A67u, 0x9D90154Au, 0x1B7F37C3u);
        public readonly Dictionary<FTextKey, Dictionary<FTextKey, FEntry>> Entries = new ();

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
                throw new ParserException(Ar, $"LocRes '{Ar.Name}' is too new to be loaded (File Version: {versionNumber:D}, Loader Version: {ELocResVersion.Latest:D})");
            }

            // Read the localized string array
            var localizedStringArray = Array.Empty<FTextLocalizationResourceString>();
            if (versionNumber >= ELocResVersion.Compact)
            {
                var localizedStringArrayOffset = Ar.Read<long>();
                if (localizedStringArrayOffset != -1) // INDEX_NONE
                {
                    var currentFileOffset = Ar.Position;
                    Ar.Position = localizedStringArrayOffset;
                    if (versionNumber >= ELocResVersion.Optimized_CRC32)
                    {
                        localizedStringArray = Ar.ReadArray(() => new FTextLocalizationResourceString(Ar));
                    }
                    else
                    {
                        var tmpLocalizedStringArray = Ar.ReadArray(Ar.ReadFString);
                        localizedStringArray = new FTextLocalizationResourceString[tmpLocalizedStringArray.Length];
                        for (var i = 0; i < localizedStringArray.Length; i++)
                        {
                            localizedStringArray[i] = new FTextLocalizationResourceString(tmpLocalizedStringArray[i], -1);
                        }
                    }

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
                    FEntry newEntry = new(Ar) {SourceStringHash = Ar.Read<uint>()};
                    if (versionNumber >= ELocResVersion.Compact)
                    {
                        var localizedStringIndex = Ar.Read<int>();
                        if (localizedStringArray.Length > localizedStringIndex)
                        {
                            // Steal the string if possible
                            var localizedString = localizedStringArray[localizedStringIndex];
                            if (localizedString.RefCount == 1)
                            {
                                newEntry.LocalizedString = localizedString.String;
                                localizedString.RefCount--;
                            }
                            else
                            {
                                newEntry.LocalizedString = localizedString.String;
                                if (localizedString.RefCount != -1)
                                {
                                    localizedString.RefCount--;
                                }
                            }
                        }
                        else
                        {
                            Log.Warning($"LocRes '{newEntry.LocResName}' has an invalid localized string index for namespace '{namespce.Str}' and key '{key.Str}'. This entry will have no translation.");
                        }
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
    }
}
