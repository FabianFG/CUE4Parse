using CUE4Parse.UE4.Assets.Readers;
using System.Collections.Generic;
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
            var value = Ar.ReadFString();
            if (Ar.Game == EGame.GAME_MarvelRivals) Ar.Position += 4;
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
