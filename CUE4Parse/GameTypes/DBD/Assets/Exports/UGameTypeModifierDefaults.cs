using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.GameTypes.DBD.Assets.Exports;

public class UGameTypeModifierDefaults : UDataAsset
{
    public Dictionary<EGameTypeModifierName, FTypeFlexibleTunable> Modifiers = [];

    public int GetIntModifier(EGameTypeModifierName name, int defaultValue = 0) => Modifiers.TryGetValue(name, out var modifier) && modifier.SetType is EFlexibleTunableType.IntTunable ? modifier._intValue : defaultValue;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Game is GAME_DeadByDaylight)
            Modifiers = GetOrDefault(nameof(Modifiers), Modifiers);
    }
}
