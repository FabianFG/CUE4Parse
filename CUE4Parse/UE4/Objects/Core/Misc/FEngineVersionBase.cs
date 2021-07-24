using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Core.Misc
{
    /** Enum for the components of a version string. */
    public enum EVersionComponent {
        /** Major version increments introduce breaking API changes. */
        Major,
        /** Minor version increments add additional functionality without breaking existing APIs. */
        Minor,
        /** Patch version increments fix existing functionality without changing the API. */
        Patch,
        /** The pre-release field adds additional versioning through a series of comparable dotted strings or numbers. */
        Changelist,
        Branch
    }

    public class FEngineVersionBase
    {
        /** Major version number. */
        public ushort Major;

        /** Minor version number. */
        public ushort Minor;

        /** Patch version number. */
        public ushort Patch;

        /** Changelist number. This is used to arbitrate when Major/Minor/Patch version numbers match. */
        public uint Changelist => _changelist & 0x7fffffffu; // Mask to ignore licensee bit
        protected uint _changelist;

        public FEngineVersionBase(FArchive Ar)
        {
            Major = Ar.Read<ushort>();
            Minor = Ar.Read<ushort>();
            Patch = Ar.Read<ushort>();
            _changelist = Ar.Read<uint>();
        }

        public FEngineVersionBase(ushort major, ushort minor, ushort patch, uint changelist)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            _changelist = changelist;
        }

        /** Checks if the changelist number represents licensee changelist number. */
        public bool IsLicenseeVersion()
        {
            return (_changelist & 0x80000000u) != 0u;
        }

        /** Returns whether the current version is empty. */
        public bool IsEmpty()
        {
            return Major == 0u && Minor == 0u && Patch == 0u;
        }

        /** Returns whether the engine version has a changelist component. */
        public bool HasChangelist()
        {
            return Changelist != 0;
        }

        /** Encodes a licensee changelist number (by setting the top bit) */
        public static uint EncodeLicenseeChangeList(uint changelist)
        {
            return changelist | 0x80000000u;
        }
    }
}