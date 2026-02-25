using System.Collections.Generic;
using CUE4Parse.GameTypes.CodeVein2.Encryption;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Internationalization;

public class FStringTable
{
    public string TableNamespace;
    public Dictionary<string, string> KeysToEntries;
    public Dictionary<string, Dictionary<FName, string>>? KeysToMetaData;

    public FStringTable(FAssetArchive Ar)
    {
        TableNamespace = Ar.ReadFString();

        KeysToEntries = Ar.ReadMap(Ar.ReadFString, () =>
        {
            if (Ar.Game is EGame.GAME_CodeVein2) return CodeVein2StringEncryption.CodeVein2EncryptedFString(Ar, ECV2DecryptionMode.StringTable);
            var value = Ar.ReadFString();
            if (Ar.Game == EGame.GAME_MarvelRivals) Ar.SkipFString();
            if (Ar.Game == EGame.GAME_LostRecordsBloomAndRage)
            {
                Ar.SkipFString();
                var length = int.TryParse(Ar.ReadFString(), out var len) ? len : 0;
                for (var i = 0; i < length; i++)
                    Ar.SkipFString();
            }
            return value;
        });
        if (Ar.Game == EGame.GAME_Wildgate) return;
        KeysToMetaData = Ar.ReadMap(Ar.ReadFString, () => Ar.ReadMap(Ar.ReadFName, Ar.ReadFString));
    }
}
