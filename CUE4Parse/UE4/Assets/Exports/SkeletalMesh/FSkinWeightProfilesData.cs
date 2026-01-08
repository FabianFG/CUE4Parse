using System.Collections.Generic;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

public class FSkinWeightProfilesData
{
    public readonly Dictionary<FName, FRuntimeSkinWeightProfileData> OverrideData;

    public FSkinWeightProfilesData(FArchive Ar)
    {
        OverrideData = Ar.ReadMap(Ar.ReadFName, () => new FRuntimeSkinWeightProfileData(Ar));
    }
}
