using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Sound;

public class USoundClass : UObject
{
    public Dictionary<FPackageIndex, FSoundEditorData>? EditorData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.SOUND_CLASS_SERIALISATION_UPDATE && Ar.Ver < EUnrealEngineObjectUE4Version.SOUND_CLASS_GRAPH_EDITOR)
        {
            EditorData = Ar.ReadMap(() => new FPackageIndex(Ar), () => Ar.Read<FSoundEditorData>());
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (EditorData?.Count > 0)
        {
            writer.WritePropertyName("EditorData");
            serializer.Serialize(writer, EditorData);
        }
    }
}
