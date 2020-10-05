using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using System.Collections.Generic;

namespace CUE4Parse.UE4.Assets.Exports.Sound
{
    public class FFormatContainer
    {
        public SortedDictionary<FName, FByteBulkData> Formats;

        public FFormatContainer(FAssetArchive Ar)
        {
            int numFormats = Ar.Read<int>();
            Formats = new SortedDictionary<FName, FByteBulkData>();
            for (int i = 0; i < numFormats; i++)
            {
                Formats[Ar.ReadFName()] = new FByteBulkData(Ar);
            }
        }
    }
}
