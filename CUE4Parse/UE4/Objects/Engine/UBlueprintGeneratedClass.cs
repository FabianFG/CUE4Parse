using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Engine;

public class UBlueprintGeneratedClass : UClass
{
    public Dictionary<FName, string>? EditorTags;

    public int NumReplicatedProperties;
    public FPackageIndex?[] DynamicBindingObjects = [];
    public FPackageIndex?[] ComponentTemplates = [];
    public FPackageIndex?[] Timelines = [];
    // public FBPComponentClassOverride[] ComponentClassOverrides = [];
    // public FFieldNotificationId[] FieldNotifies = [];
    public FPackageIndex? SimpleConstructionScript;
    public FPackageIndex? InheritableComponentHandler;
    public FPackageIndex? UberGraphFunction;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        NumReplicatedProperties = GetOrDefault<int>(nameof(NumReplicatedProperties));
        DynamicBindingObjects = GetOrDefault(nameof(DynamicBindingObjects), DynamicBindingObjects);
        ComponentTemplates = GetOrDefault(nameof(ComponentTemplates), ComponentTemplates);
        Timelines = GetOrDefault(nameof(Timelines), Timelines);
        SimpleConstructionScript = GetOrDefault<FPackageIndex?>(nameof(SimpleConstructionScript));
        InheritableComponentHandler = GetOrDefault<FPackageIndex?>(nameof(InheritableComponentHandler));
        UberGraphFunction = GetOrDefault<FPackageIndex?>(nameof(UberGraphFunction));

        if (Ar.Game == EGame.GAME_WorldofJadeDynasty) Ar.Position += 24;
        if (FFortniteMainBranchObjectVersion.Get(Ar) >= FFortniteMainBranchObjectVersion.Type.BPGCCookedEditorTags)
        {
            if (validPos - Ar.Position > 4)
            {
                EditorTags = Ar.ReadMap(Ar.ReadFName, Ar.ReadFString);
            }
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (EditorTags is not { Count: > 0 }) return;
        writer.WritePropertyName("EditorTags");
        serializer.Serialize(writer, EditorTags);
    }
}
