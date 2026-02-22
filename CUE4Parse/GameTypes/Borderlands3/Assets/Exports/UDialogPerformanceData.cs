using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.Borderlands3.Assets.Exports;

public class UDialogPerformanceData : UObject
{
    public FText? Text;
    public UObject? WwiseExternalMediaTemplate;
    public float EstimatedDuration;
    public uint WwiseEventShortID;
    public FName MoodName;
    public UObject? Style;
    public FStructFallback? QuietTime;
    public FGuid Guid;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        Text = GetOrDefault<FText>(nameof(Text));
        WwiseExternalMediaTemplate = GetOrDefault<UObject>(nameof(WwiseExternalMediaTemplate));
        EstimatedDuration = GetOrDefault<float>(nameof(EstimatedDuration));
        WwiseEventShortID = GetOrDefault<uint>(nameof(WwiseEventShortID));
        MoodName = GetOrDefault<FName>(nameof(MoodName));
        Style = GetOrDefault<UObject>(nameof(Style));
        QuietTime = GetOrDefault<FStructFallback>(nameof(QuietTime));
        Guid = GetOrDefault<FGuid>(nameof(Guid));
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
    }
}
