using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Core.Misc
{
    public class FEngineVersion : FEngineVersionBase
    {
        /** Branch name. */
        public string Branch => _branch.Replace('+', '/');
        private string _branch;

        public FEngineVersion(FArchive Ar) : base(Ar)
        {
            _branch = Ar.ReadFString();
        }

        public FEngineVersion(ushort major, ushort minor, ushort patch, uint changelist, string branch) : base(major, minor, patch, changelist)
        {
            _branch = branch.Replace('/', '+');;
        }

        public void Set(ushort major, ushort minor, ushort patch, uint changelist, string branch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            _changelist = changelist;
            _branch = branch.Replace('/', '+');
        }

        public string ToString(EVersionComponent lastComponent)
        {
            var result = Major.ToString();
            if (lastComponent >= EVersionComponent.Minor)
            {
                result += "." + Minor;
                if (lastComponent >= EVersionComponent.Patch)
                {
                    result += "." + Patch;
                    if (lastComponent >= EVersionComponent.Changelist)
                    {
                        result += "-" + Changelist;
                        if (lastComponent >= EVersionComponent.Branch && _branch.Length > 0)
                        {
                            result += "+" + _branch;
                        }
                    }
                }
            }
            return result;
        }

        public override string ToString() => ToString(EVersionComponent.Branch);
    }
}