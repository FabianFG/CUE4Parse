using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using System.Collections.Generic;

namespace CUE4Parse.UE4.Assets.Objects
{
    public class FStructFallback : IUStruct
    {
        public List<FPropertyTag> Properties { get; }
        public FGuid? ObjectGuid { get; }

        public FStructFallback(FAssetArchive Ar)
        {
            Properties = new List<FPropertyTag>();
            while (true)
            {
                var tag = new FPropertyTag(Ar, true);
                if (tag.Name.IsNone)
                    break;
                Properties.Add(tag);
            }
        }
    }
}
