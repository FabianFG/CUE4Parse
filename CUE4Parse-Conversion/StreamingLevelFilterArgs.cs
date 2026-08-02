using CUE4Parse_Conversion.Dto;

namespace CUE4Parse_Conversion;

public readonly struct StreamingLevelFilterArgs(string worldName, IReadOnlyList<ActorDto> actors, IReadOnlyList<StreamingLevel> streamingLevels)
{
    public readonly string WorldName = worldName;

    public readonly IReadOnlyList<ActorDto> Actors = actors;
    public readonly IReadOnlyList<StreamingLevel> StreamingLevels = streamingLevels;
}
