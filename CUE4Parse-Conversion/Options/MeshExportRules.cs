using CUE4Parse.UE4.Versions;
using CUE4Parse_Conversion.Dto;

namespace CUE4Parse_Conversion.Options;

/// <summary>Additional rules for exporting.</summary>
public sealed class MeshExportRules(EGame? game, MeshMaterialDto[] materials)
{
    public MeshExportRules(EGame? game) : this(game, []) { }

    public bool UseDiffuseColor => game is not EGame.GAME_DeadByDaylight;

    public bool ShouldExportSection(MeshSectionDto section)
    {
        if (section.bDisabled)
            return false;

        // Lambert sections in DBD are for cloth physics
        if (game is not EGame.GAME_DeadByDaylight || section.MaterialIndex < 0 || section.MaterialIndex >= materials.Length)
            return true;

        var slot = materials[section.MaterialIndex];
        return slot.Material is not { IsNull: true } || !slot.SlotName.StartsWith("lambert", StringComparison.OrdinalIgnoreCase);
    }
}
