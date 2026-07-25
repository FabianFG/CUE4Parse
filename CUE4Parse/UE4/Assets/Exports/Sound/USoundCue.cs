using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Sound;

public class USoundCue : USoundBase
{
    public FPackageIndex? FirstNode;
    public float VolumeMultiplier;
    public float PitchMultiplier;
    public Dictionary<FPackageIndex, FSoundEditorData>? EditorData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        FirstNode = GetOrDefault<FPackageIndex>(nameof(FirstNode));
        VolumeMultiplier = GetOrDefault(nameof(VolumeMultiplier), 0.75f);
        PitchMultiplier = GetOrDefault(nameof(PitchMultiplier), 1f);

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.CHANGED_COMPRESSION_CHUNK_SIZE_TO_128 && Ar.Ver < EUnrealEngineObjectUE4Version.SOUND_NODE_INHERIT_FROM_ED_GRAPH_NODE)
        {
            EditorData = Ar.ReadMap(() => new FPackageIndex(Ar), () => Ar.Read<FSoundEditorData>());
        }

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.COOKED_ASSETS_IN_EDITOR_SUPPORT)
            _ = new FStripDataFlags(Ar);
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
