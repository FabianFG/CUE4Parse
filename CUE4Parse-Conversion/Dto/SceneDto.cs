using CUE4Parse.UE4.Assets.Exports;

namespace CUE4Parse_Conversion.Dto;

public abstract class SceneDto(UObject owner) : ObjectDto(owner)
{
    public readonly List<ActorDto> Actors = [];
    public readonly List<StreamingLevel> StreamingLevels = [];

    public bool IsEmpty => Actors.Count == 0 && StreamingLevels.Count == 0;

    public override string ToString() => $"{base.ToString()} (Actors: {Actors.Count}, StreamingLevels: {StreamingLevels.Count})";

    public override void Dispose()
    {
        foreach (var actor in Actors)
        {
            actor.Dispose();
        }
        Actors.Clear();
        StreamingLevels.Clear();
    }
}
