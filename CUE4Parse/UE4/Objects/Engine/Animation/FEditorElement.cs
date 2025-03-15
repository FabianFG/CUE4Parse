using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Objects.Engine.Animation;

[StructFallback]
public readonly struct FEditorElement
{
    public const int MAX_VERTICES = 3;

    public readonly int[] Indices;
    public readonly float[] Weights;

    public FEditorElement()
    {
        Indices = new int[MAX_VERTICES];
        Weights = new float[MAX_VERTICES];

        for (var elementIndex = 0; elementIndex < MAX_VERTICES; elementIndex++)
        {
            Indices[elementIndex] = -1;
            Weights[elementIndex] = 0;
        }
    }

    public FEditorElement(FStructFallback data) : this()
    {
        data.TryGetAllValues(out Indices, nameof(Indices));
        data.TryGetAllValues(out Weights, nameof(Weights));
    }
}
