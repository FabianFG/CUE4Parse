using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Core.Misc
{
    public class FEngineVersion : FEngineVersionBase
    {
        /** Branch name. */
        public readonly string Branch;

        public FEngineVersion(FArchive Ar) : base(Ar)
        {
            Branch = Ar.ReadFString();
        }

        public FEngineVersion(ushort major, ushort minor, ushort patch, uint changeList, string branch) : base(major, minor, patch, changeList)
        {
            Branch = branch;
        }

        public override string ToString() => $"{base.ToString()}, {nameof(Branch)}: {Branch}";
    }
}