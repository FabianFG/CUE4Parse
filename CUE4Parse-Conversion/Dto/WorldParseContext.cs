using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.Dto;

internal sealed class WorldParseContext
{
    /// <summary>
    /// flattened list of resolved components keyed by their unresolved pointer
    /// </summary>
    private readonly Dictionary<FPackageIndex, ComponentDto> _registry = new();

    public ComponentDto? GetOrCreate(FPackageIndex? ptr, ActorDto owner)
    {
        if (ptr is not { IsNull: false }) return null;
        if (_registry.TryGetValue(ptr, out var existing)) return existing;

        var dto = ComponentDto.Create(ptr, owner);
        if (dto is null) return null;

        _registry[ptr] = dto;

        if (dto is SceneComponentDto { _attachParent: { IsNull: false } parentPtr } scene && GetOrCreate(parentPtr, owner) is SceneComponentDto parentScene)
        {
            parentScene.AddChildComponent(scene);
        }

        return dto;
    }
}
