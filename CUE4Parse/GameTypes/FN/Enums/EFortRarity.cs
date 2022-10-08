using System.ComponentModel;
using CUE4Parse.UE4.Objects.Core.i18N;

namespace CUE4Parse.GameTypes.FN.Enums
{
    public enum EFortRarity : byte
    {
        [Description("Uncommon")]
        Uncommon = 1, // Default

        [Description("Unattainable")]
        Impossible = 7,
        [Description("Unattainable")]
        Unattainable = 7,

        [Description("Exotic")]
        Exotic = 6,
        [Description("Exotic")]
        Transcendent = 6,

        [Description("Mythic")]
        Elegant = 5,
        [Description("Mythic")]
        Mythic = 5,

        [Description("Legendary")]
        Fine = 4,
        [Description("Legendary")]
        Legendary = 4,

        [Description("Epic")]
        Quality = 3,
        [Description("Epic")]
        Epic = 3,

        [Description("Rare")]
        Sturdy = 2,
        [Description("Rare")]
        Rare = 2,

        [Description("Common")]
        Handmade = 0,
        [Description("Common")]
        Common = 0
    }

    public static class RarityUtil
    {
        private static readonly FText _unattainable = new FText("Fort.Rarity", "Unattainable", "Unattainable");
        private static readonly FText _transcendent = new FText("Fort.Rarity", "Transcendent", "Transcendent");
        private static readonly FText _mythic = new FText("Fort.Rarity", "Mythic", "Mythic");
        private static readonly FText _legendary = new FText("Fort.Rarity", "Legendary", "Legendary");
        private static readonly FText _epic = new FText("Fort.Rarity", "Epic", "Epic");
        private static readonly FText _rare = new FText("Fort.Rarity", "Rare", "Rare");
        private static readonly FText _uncommon = new FText("Fort.Rarity", "Uncommon", "Uncommon");
        private static readonly FText _common = new FText("Fort.Rarity", "Common", "Common");

        public static FText GetNameText(this EFortRarity rarity) => rarity switch
        {
            EFortRarity.Uncommon => _uncommon,
            EFortRarity.Unattainable => _unattainable,
            EFortRarity.Transcendent => _transcendent,
            EFortRarity.Mythic => _mythic,
            EFortRarity.Legendary => _legendary,
            EFortRarity.Epic => _epic,
            EFortRarity.Rare => _rare,
            EFortRarity.Common => _common,
            _ => _uncommon
        };
    }
}
