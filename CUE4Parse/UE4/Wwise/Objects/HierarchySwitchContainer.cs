using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects
{
    public class HierarchySwitchContainer : AbstractHierarchy
    {
        public HierarchySwitchContainer(FArchive Ar, long hierarchyEndPosition) : base(Ar)
        {
            Ar.Position = hierarchyEndPosition;
        }

        public override void WriteJson(JsonWriter writer, JsonSerializer serializer) { }
    }
}
