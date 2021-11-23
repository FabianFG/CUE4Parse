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
        public static int CustomVer(this FArchive Ar, FGuid key)
        {
            var packageSummary = (Ar as FAssetArchive)?.Owner.Summary;
            if (packageSummary?.CustomVersionContainer != null && !packageSummary.bUnversioned)
            {
                var packageCustomVersion = packageSummary.CustomVersionContainer.GetVersion(key);
                return packageCustomVersion != -1 ? packageCustomVersion : 0; // Explicitly set to BeforeCustomVersionWasAdded if not found
            }

            var overrideCustomVersions = Ar.Versions.CustomVersions;
            if (overrideCustomVersions != null)
            {
                return overrideCustomVersions.GetVersion(key); // Will return determine by game if not found
            }

            return -1; // Determine by game
        }

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