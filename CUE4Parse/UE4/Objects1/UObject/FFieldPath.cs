using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

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

            if (FFortniteMainBranchObjectVersion.Get(Ar) >= FFortniteMainBranchObjectVersion.Type.FFieldPathOwnerSerialization || FReleaseObjectVersion.Get(Ar) >= FReleaseObjectVersion.Type.FFieldPathOwnerSerialization)
            {
                ResolvedOwner = new FPackageIndex(Ar);
            }
        }

        public FFieldPath(FKismetArchive Ar)
        {
            var index = Ar.Index;
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

            if (FFortniteMainBranchObjectVersion.Get(Ar) >= FFortniteMainBranchObjectVersion.Type.FFieldPathOwnerSerialization || FReleaseObjectVersion.Get(Ar) >= FReleaseObjectVersion.Type.FFieldPathOwnerSerialization)
            {
                ResolvedOwner = new FPackageIndex(Ar);
            }

            Ar.Index = index + 8;
        }

        protected internal void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            if (ResolvedOwner is null)
            {
                serializer.Serialize(writer, this);
                return;
            }

            if (ResolvedOwner.IsNull)
            {
                //if (Path.Count > 0) Log.Warning("");
                writer.WriteNull();
                return;
            }

            if (!ResolvedOwner.TryLoad<UField>(out var field))
            {
                serializer.Serialize(writer, this);
                return;
            }

            switch (field)
            {
                case UScriptClass:
                    serializer.Serialize(writer, this);
                    break;
                case UStruct struc when Path.Count > 0 && struc.GetProperty(Path[0], out var prop):
                    writer.WriteStartObject();
                    writer.WritePropertyName("Owner");
                    serializer.Serialize(writer, ResolvedOwner);
                    writer.WritePropertyName("Property");
                    serializer.Serialize(writer, prop);
                    writer.WriteEndObject();
                    break;
                default:
                    serializer.Serialize(writer, this);
                    break;
            }  
        }
    }
}
