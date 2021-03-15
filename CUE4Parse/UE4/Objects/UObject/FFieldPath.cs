using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.UObject
{
    public class FFieldPath
    {
        public List<FName> Path;
        public FPackageIndex ResolvedOwner; //UStruct

        public FFieldPath()
        {
            Path = new List<FName>();
            ResolvedOwner = new FPackageIndex();
        }

        public FFieldPath(FAssetArchive Ar)
        {
            var pathNum = Ar.Read<int>();
            Path = new List<FName>(pathNum);
            for (int i = 0; i < pathNum; i++)
            {
                Path.Add(Ar.ReadFName());
            }

            // The old serialization format could save 'None' paths, they should be just empty
            if (Path.Count == 1 && Path[0].IsNone)
            {
                Path.Clear();
            }

            //if (Ar.CustomVer(FFortniteMainBranchObjectVersion.GUID) >= FFortniteMainBranchObjectVersion.FFieldPathOwnerSerialization || Ar.CustomVer(FReleaseObjectVersion.GUID) >= FReleaseObjectVersion.FFieldPathOwnerSerialization) {
            ResolvedOwner = new FPackageIndex(Ar);
            //}
        }
    }
}