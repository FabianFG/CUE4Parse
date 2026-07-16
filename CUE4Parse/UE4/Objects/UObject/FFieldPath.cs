using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.UObject;

public class FFieldPath
{
    public FName[] Path;
    public FPackageIndex? ResolvedOwner; //UStruct

    public FFieldPath()
    {
        Path = [];
        ResolvedOwner = new FPackageIndex();
    }

    public FFieldPath(FAssetArchive Ar) : this()
    {
        Path = Ar.ReadArray(() => Ar.ReadFName());
        // The old serialization format could save 'None' paths, they should be just empty
        if (Path.Length == 1 && Path[0].IsNone) Path = [];

        if (FFortniteMainBranchObjectVersion.Get(Ar) >= FFortniteMainBranchObjectVersion.Type.FFieldPathOwnerSerialization ||
            FReleaseObjectVersion.Get(Ar) >= FReleaseObjectVersion.Type.FFieldPathOwnerSerialization)
        {
            ResolvedOwner = new FPackageIndex(Ar);
        }
    }

    public FFieldPath(FKismetArchive Ar) : this()
    {
        var index = Ar.Index;
        Path = Ar.ReadArray(Ar.ReadFName);
        // The old serialization format could save 'None' paths, they should be just empty
        if (Path.Length == 1 && Path[0].IsNone) Path = [];

        if (FFortniteMainBranchObjectVersion.Get(Ar) >= FFortniteMainBranchObjectVersion.Type.FFieldPathOwnerSerialization ||
            FReleaseObjectVersion.Get(Ar) >= FReleaseObjectVersion.Type.FFieldPathOwnerSerialization)
        {
            ResolvedOwner = new FPackageIndex(Ar);
        }

        Ar.Index = index + 8;
    }

    public override string ToString()
    {
        return Path.Length == 0 ? string.Empty : Path[0].ToString();
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
            case UStruct struc when Path.Length > 0 && struc.GetProperty(Path[0], out var prop):
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
