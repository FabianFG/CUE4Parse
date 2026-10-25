using System.ComponentModel;
using CUE4Parse.UE4.Objects.Core.i18N;

namespace CUE4Parse.GameTypes.SJ.Enums;

public enum ESpyjinxRarity : byte
{
    [Description("Common")]
    Common = 0,

    [Description("Uncommon")]
    Uncommon = 1,

    [Description("Rare")]
    Rare = 2,

    [Description("Epic")]
    Epic = 3,

    [Description("Legendary")]
    Legendary = 4
}
public static class RarityUtil
{
    private static readonly FText _legendary = new FText("Spyjinx.Rarity", "Legendary", "Legendary");
    private static readonly FText _epic = new FText("Spyjinx.Rarity", "Epic", "Epic");
    private static readonly FText _rare = new FText("Spyjinx.Rarity", "Rare", "Rare");
    private static readonly FText _uncommon = new FText("Spyjinx.Rarity", "Uncommon", "Uncommon");
    private static readonly FText _common = new FText("Spyjinx.Rarity", "Common", "Common");

    public static FText GetNameText(this ESpyjinxRarity rarity) => rarity switch
    {
        ESpyjinxRarity.Uncommon => _uncommon,
        ESpyjinxRarity.Legendary => _legendary,
        ESpyjinxRarity.Epic => _epic,
        ESpyjinxRarity.Rare => _rare,
        ESpyjinxRarity.Common => _common,
        _ => _common
    };
}
