using CUE4Parse.UE4.Versions;
using CUE4Parse_Conversion.Dto;

namespace CUE4Parse_Conversion.Options;

/// <summary>Additional rules for exporting.</summary>
public sealed class MeshExportRules(EGame game, bool exportMaterials)
{
    public bool PreserveVertexColorMasks => exportMaterials;

    // Lambert materials in DBD are for cloth physics
    public bool ShouldExportSection(MeshMaterialDto? slot)
    {
        if (!exportMaterials || game != EGame.GAME_DeadByDaylight)
            return true;
        if (slot is not { Material.IsNull: true } helper)
            return true;

        return !helper.SlotName.StartsWith("lambert", StringComparison.OrdinalIgnoreCase);
    }
}
