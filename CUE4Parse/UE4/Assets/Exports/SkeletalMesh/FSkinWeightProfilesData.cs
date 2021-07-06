using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    public class FSkinWeightProfilesData
    {
        public readonly Dictionary<FName, FRuntimeSkinWeightProfileData> OverrideData;

        public FSkinWeightProfilesData(FAssetArchive Ar)
        {
            var length = Ar.Read<int>();
            OverrideData = new Dictionary<FName, FRuntimeSkinWeightProfileData>();
            for (var i = 0; i < length; i++)
            {
                OverrideData[Ar.ReadFName()] = new FRuntimeSkinWeightProfileData(Ar);
            }
        }
    }
}