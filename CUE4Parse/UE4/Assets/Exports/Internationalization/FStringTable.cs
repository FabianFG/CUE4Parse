using CUE4Parse.UE4.Assets.Readers;
using System.Collections.Generic;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Internationalization;

public class FStringTable
{
    public string TableNamespace;
    public Dictionary<string, string> KeysToEntries;
    public Dictionary<string, Dictionary<FName, string>> KeysToMetaData;

    public FStringTable(FAssetArchive Ar)
    {
        TableNamespace = Ar.ReadFString();

        KeysToEntries = Ar.ReadMap(Ar.ReadFString, Ar.ReadFString);
        KeysToMetaData = Ar.ReadMap(Ar.ReadFString, () => Ar.ReadMap(Ar.ReadFName, Ar.ReadFString));
    }
}
