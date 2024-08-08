using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions;

public static class VersionUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CustomVer(this FArchive Ar, FGuid key)
    {
        var overrideCustomVersions = Ar.Versions.CustomVersions;
        if (overrideCustomVersions != null)
        {
            var overrideCustomVersion = overrideCustomVersions.GetVersion(key);
            if (overrideCustomVersion != -1)
                return overrideCustomVersion; // Return only if override
        }

        var packageSummary = (Ar as FAssetArchive)?.Owner.Summary;
        if (packageSummary is { bUnversioned: false })
        {
            var packageCustomVersion = packageSummary.CustomVersionContainer.GetVersion(key);
            return packageCustomVersion; // proceed so we can guess version from engine version
        }

        return -1; // Determine by game
    }
}
