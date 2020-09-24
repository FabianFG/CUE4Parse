using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports
{
    public class UObject : UExport
    {
        public List<FPropertyTag> Properties { get; }
        public bool ReadGuid { get; }
        public FGuid? ObjectGuid { get; private set; }

        public UObject(FObjectExport exportObject, bool readGuid = true) : base(exportObject)
        {
            Properties = new List<FPropertyTag>();
            ReadGuid = readGuid;
        }

        public UObject() : this(new List<FPropertyTag>(), null, "")
        {
            ExportType = GetType().Name;
            Name = ExportType;
        }

        public UObject(List<FPropertyTag> properties, FGuid? objectGuid, string exportType) : base(exportType)
        {
            Properties = properties;
            ObjectGuid = objectGuid;
        }

        public override void Deserialize(FAssetArchive Ar)
        {
            while (true)
            {
                var tag = new FPropertyTag(Ar, true);
                if (tag.Name.IsNone)
                    break;
                Properties.Add(tag);
            }

            if (ReadGuid && Ar.ReadBoolean() && Ar.Position + 16 <= Ar.Length)
            {
                ObjectGuid = Ar.Read<FGuid>();
            }
        }
    }
}