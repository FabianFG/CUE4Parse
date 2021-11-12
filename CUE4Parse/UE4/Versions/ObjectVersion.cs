using System;

namespace CUE4Parse.UE4.Versions
{
    public enum EUnrealEngineObjectUE5Version : uint
    {
        // Note that currently the oldest loadable package version is EUnrealEngineObjectUE4Version.VER_UE4_OLDEST_LOADABLE_PACKAGE
        // this can be enabled should we ever deprecate UE4 versions entirely
        //OLDEST_LOADABLE_PACKAGE = ???,

        // The original UE5 version, at the time this was added the UE4 version was 522, so UE5 will start from 1000 to show a clear difference
        INITIAL_VERSION = 1000,

        // Support stripping names that are not referenced from export data
        NAMES_REFERENCED_FROM_EXPORT_DATA,

        // Added a payload table of contents to the package summary 
        PAYLOAD_TOC,

        // -----<new versions can be added before this line>-------------------------------------------------
        // - this needs to be the last line (see note below)
        AUTOMATIC_VERSION_PLUS_ONE,
        AUTOMATIC_VERSION = AUTOMATIC_VERSION_PLUS_ONE - 1
    }

    public enum EUnrealEngineObjectLicenseeUEVersion
    {
        VER_LIC_NONE = 0,

        // - this needs to be the last line (see note below)
        VER_LIC_AUTOMATIC_VERSION_PLUS_ONE,
        VER_LIC_AUTOMATIC_VERSION = VER_LIC_AUTOMATIC_VERSION_PLUS_ONE - 1
    }

    /// <summary>
    /// This object combines all of our version enums into a single easy to use structure
    /// which allows us to update older version numbers independently of the newer version numbers.
    /// </summary>
    public struct FPackageFileVersion : IComparable<EUnrealEngineObjectUE4Version>, IComparable<UE4Version>, IComparable<EUnrealEngineObjectUE5Version>
    {
        /// UE4 file version
        public int FileVersionUE4;

        /// UE5 file version
        public int FileVersionUE5;

        /// Set all versions to the default state
        public void Reset()
        {
            FileVersionUE4 = 0;
            FileVersionUE5 = 0;
        }

        public FPackageFileVersion(int ue4Version, int ue5Version)
        {
            FileVersionUE4 = ue4Version;
            FileVersionUE5 = ue5Version;
        }

        /// Creates and returns a FPackageFileVersion based on a single EUnrealEngineObjectUEVersion and no other versions.
        public static FPackageFileVersion CreateUE4Version(int version) => new(version, 0);
        public static FPackageFileVersion CreateUE4Version(EUnrealEngineObjectUE4Version version) => new((int) version, 0);
        public static FPackageFileVersion CreateUE4Version(UE4Version version) => new((int) version, 0);

        public int Value
        {
            get => FileVersionUE5 >= (int) EUnrealEngineObjectUE5Version.INITIAL_VERSION ? FileVersionUE5 : FileVersionUE4;
            set
            {
                if (value >= (int) EUnrealEngineObjectUE5Version.INITIAL_VERSION)
                {
                    FileVersionUE5 = value;
                }
                else
                {
                    FileVersionUE4 = value;
                }
            }
        }

        /// UE4 version comparisons
        public static bool operator ==(FPackageFileVersion a, EUnrealEngineObjectUE4Version b) => a.FileVersionUE4 == (int) b;
        public static bool operator !=(FPackageFileVersion a, EUnrealEngineObjectUE4Version b) => a.FileVersionUE4 != (int) b;
        public static bool operator < (FPackageFileVersion a, EUnrealEngineObjectUE4Version b) => a.FileVersionUE4 <  (int) b;
        public static bool operator > (FPackageFileVersion a, EUnrealEngineObjectUE4Version b) => a.FileVersionUE4 >  (int) b;
        public static bool operator <=(FPackageFileVersion a, EUnrealEngineObjectUE4Version b) => a.FileVersionUE4 <= (int) b;
        public static bool operator >=(FPackageFileVersion a, EUnrealEngineObjectUE4Version b) => a.FileVersionUE4 >= (int) b;
        public int CompareTo(EUnrealEngineObjectUE4Version other) => FileVersionUE4.CompareTo(other);

        public static bool operator ==(FPackageFileVersion a, UE4Version b) => a.FileVersionUE4 == (int) b;
        public static bool operator !=(FPackageFileVersion a, UE4Version b) => a.FileVersionUE4 != (int) b;
        public static bool operator < (FPackageFileVersion a, UE4Version b) => a.FileVersionUE4 <  (int) b;
        public static bool operator > (FPackageFileVersion a, UE4Version b) => a.FileVersionUE4 >  (int) b;
        public static bool operator <=(FPackageFileVersion a, UE4Version b) => a.FileVersionUE4 <= (int) b;
        public static bool operator >=(FPackageFileVersion a, UE4Version b) => a.FileVersionUE4 >= (int) b;
        public int CompareTo(UE4Version other) => FileVersionUE4.CompareTo(other);

        /// UE5 version comparisons
        public static bool operator ==(FPackageFileVersion a, EUnrealEngineObjectUE5Version b) => a.FileVersionUE5 == (int) b;
        public static bool operator !=(FPackageFileVersion a, EUnrealEngineObjectUE5Version b) => a.FileVersionUE5 != (int) b;
        public static bool operator < (FPackageFileVersion a, EUnrealEngineObjectUE5Version b) => a.FileVersionUE5 <  (int) b;
        public static bool operator > (FPackageFileVersion a, EUnrealEngineObjectUE5Version b) => a.FileVersionUE5 >  (int) b;
        public static bool operator <=(FPackageFileVersion a, EUnrealEngineObjectUE5Version b) => a.FileVersionUE5 <= (int) b;
        public static bool operator >=(FPackageFileVersion a, EUnrealEngineObjectUE5Version b) => a.FileVersionUE5 >= (int) b;
        public int CompareTo(EUnrealEngineObjectUE5Version other) => FileVersionUE5.CompareTo(other);

        /// <summary>
        /// Returns true if this object is compatible with the FPackageFileVersion passed in as the parameter.
        /// This means that  all version numbers for the current object are equal or greater than the
        /// corresponding version numbers of the other structure.
        /// </summary>
        public bool IsCompatible(FPackageFileVersion other) => FileVersionUE4 >= other.FileVersionUE4 && FileVersionUE5 >= other.FileVersionUE5;

        /// FPackageFileVersion comparisons
        public static bool operator ==(FPackageFileVersion a, FPackageFileVersion b) => a.FileVersionUE4 == b.FileVersionUE4 && a.FileVersionUE5 == b.FileVersionUE5;
        public static bool operator !=(FPackageFileVersion a, FPackageFileVersion b) => !(a == b);
        public override bool Equals(object? obj) => obj is FPackageFileVersion other && this == other;
        public override int GetHashCode() => HashCode.Combine(FileVersionUE4, FileVersionUE5);
    }
}