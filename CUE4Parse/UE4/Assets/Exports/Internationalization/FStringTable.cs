using CUE4Parse.UE4.Assets.Readers;
using System.Collections.Generic;

namespace CUE4Parse.UE4.Assets.Exports.Internationalization
{
    public class FStringTable
    {
        public string TableNamespace;
        public Dictionary<string, string> KeysToMetaData;

        public FStringTable(FAssetArchive Ar)
        {
            TableNamespace = Ar.ReadFString();
            
            var numEntries = Ar.Read<int>();
            KeysToMetaData = new Dictionary<string, string>(numEntries);
            for (var i = 0; i < numEntries; ++i)
            {
                KeysToMetaData[Ar.ReadFString()] = Ar.ReadFString();
            }
        }
    }
}
