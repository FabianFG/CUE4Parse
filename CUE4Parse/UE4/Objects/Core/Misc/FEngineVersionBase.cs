using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Core.Misc
{
    [StructLayout(LayoutKind.Sequential)]
    public class FEngineVersionBase
    {
        /** Major version number. */
        public readonly ushort Major;

        /** Minor version number. */
        public readonly ushort Minor;

        /** Patch version number. */
        public readonly ushort Patch;

        /** Changelist number. This is used to arbitrate when Major/Minor/Patch version numbers match. */
        private readonly uint _changeList;

        public uint ChangeList => _changeList & 0x7fffffffu; // Mask to ignore licensee bit

        public FEngineVersionBase(FArchive Ar)
        {
            Major = Ar.Read<ushort>();
            Minor = Ar.Read<ushort>();
            Patch = Ar.Read<ushort>();
            _changeList = Ar.Read<uint>();
        }

        public FEngineVersionBase(ushort major, ushort minor, ushort patch, uint changeList)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            _changeList = changeList;
        }

        /** Checks if the changelist number represents licensee changelist number. */
        public bool IsLicenseeVersion()
        {
            return (ChangeList & 0x80000000u) != 0u;
        }

        /** Returns whether the current version is empty. */
        public bool IsEmpty()
        {
            return Major == 0u && Minor == 0u && Patch == 0u;
        }

        /** Returns whether the engine version has a changelist component. */
        public bool HasChangeList()
        {
            return ChangeList != 0;
        }

        /** Encodes a licensee changelist number (by setting the top bit) */
        public static uint encodeLicenseeChangeList(uint changeList)
        {
            return changeList | 0x80000000u;
        }

        public override string ToString() =>
            $"{nameof(Major)}: {Major}, {nameof(Minor)}: {Minor}, {nameof(Patch)}: {Patch}, {nameof(ChangeList)}: {ChangeList}";
    }
}