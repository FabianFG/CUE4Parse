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
    private static readonly FText _legendary = new FText("Spyjinx.Rarity", "Legendary", "4");
    private static readonly FText _epic = new FText("Spyjinx.Rarity", "Epic", "3");
    private static readonly FText _rare = new FText("Spyjinx.Rarity", "Rare", "2");
    private static readonly FText _uncommon = new FText("Fort.Rarity", "Uncommon", "1");
    private static readonly FText _common = new FText("Fort.Rarity", "Common", "0");

    public static FText GetNameText(this ESpyjinxRarity rarity) => rarity switch
    {
        ESpyjinxRarity.Uncommon => _uncommon,
        ESpyjinxRarity.Legendary => _legendary,
        ESpyjinxRarity.Epic => _epic,
        ESpyjinxRarity.Rare => _rare,
        ESpyjinxRarity.Common => _common,
        _ => _uncommon
    };
}
