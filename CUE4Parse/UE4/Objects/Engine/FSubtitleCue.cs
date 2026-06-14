using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.i18N;

namespace CUE4Parse.UE4.Objects.Engine;

[StructFallback]
public class FSubtitleCue
{
    public FText Text;
    public float Time;
    
    public FSubtitleCue(FStructFallback fallback)
    {
        Text = fallback.GetOrDefault<FText>(nameof(Text));
        Time = fallback.GetOrDefault<float>(nameof(Time));
    }
}