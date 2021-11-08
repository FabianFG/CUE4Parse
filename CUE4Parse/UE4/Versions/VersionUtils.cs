using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Core.Serialization;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions
{
    public static class VersionUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CustomVer(this FArchive Ar, FGuid key) =>
            Ar.Versions.CustomVersions?.GetVersion(key) ?? (Ar as FAssetArchive)?.Owner.Summary.CustomVersionContainer.GetVersion(key) ?? -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetVersion(this IList<FCustomVersion> customVersions, FGuid customKey)
        {
            for (var i = 0; i < customVersions.Count; i++)
            {
                if (customVersions[i].Key == customKey)
                {
                    return customVersions[i].Version;
                }
            }

            return -1;
        }
    }
}