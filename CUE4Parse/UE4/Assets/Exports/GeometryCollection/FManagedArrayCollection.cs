using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.GeometryCollection
{
    public class FManagedArrayCollection
    {
        public readonly int Version;
        public readonly Dictionary<FName, int> GroupInfo;       //FGroupInfo
        public readonly Dictionary<FKeyType, FValueType> Map;

        public FManagedArrayCollection(FAssetArchive Ar)
        {
            Version = Ar.Read<int>();

            var mapLength = Ar.Read<int>();
            GroupInfo = new Dictionary<FName, int>(mapLength);
            for (int i = 0; i < mapLength; i++)
            {
                GroupInfo[Ar.ReadFName()] = Ar.Read<int>();
            }

            mapLength = Ar.Read<int>();
            Map = new Dictionary<FKeyType, FValueType>(mapLength);
            for (int i = 0; i < mapLength; i++)
            {
                Map[new FKeyType(Ar)] = new FValueType(Ar, Version);
            }
        }
    }
}
