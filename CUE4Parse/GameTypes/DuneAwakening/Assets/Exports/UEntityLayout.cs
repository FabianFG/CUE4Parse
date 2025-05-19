using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.DuneAwakening.Assets.Exports;

public class UEntityLayout : UObject
{
    public FPackageIndex m_FlatLayout;
    public FSoftObjectPath m_ParentLayout;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        m_FlatLayout = new FPackageIndex(Ar);
        m_ParentLayout = new FSoftObjectPath(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(m_FlatLayout));
        serializer.Serialize(writer, m_FlatLayout);
        writer.WritePropertyName(nameof(m_ParentLayout));
        serializer.Serialize(writer, m_ParentLayout);
    }
}
