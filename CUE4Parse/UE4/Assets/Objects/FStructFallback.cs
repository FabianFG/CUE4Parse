using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;

namespace CUE4Parse.UE4.Assets.Objects
{
    public class FStructFallback : IUStruct, IPropertyHolder
    {
        public List<FPropertyTag> Properties { get; }

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
        
        public T GetOrDefault<T>(string name, StringComparison comparisonType = StringComparison.Ordinal) =>
            PropertyUtil.GetOrDefault<T>(this, name, comparisonType);
        public T Get<T>(string name, StringComparison comparisonType = StringComparison.Ordinal) =>
            PropertyUtil.Get<T>(this, name, comparisonType);
    }
}
