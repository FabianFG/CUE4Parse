using CUE4Parse.UE4.Objects.Core.i18N;

namespace CUE4Parse_Fortnite.Enums
{
    public enum EFortRarity
    {
        Uncommon = 1, // Default
        
        Impossible = 7,
        Unattainable = 7,
        
        Exotic = 6,
        Transcendent = 6,

        Elegant = 5,
        Mythic = 5,

        Fine = 4,
        Legendary = 4,

        Quality = 3,
        Epic = 3,

        Sturdy = 2,
        Rare = 2,

        Handmade = 0,
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