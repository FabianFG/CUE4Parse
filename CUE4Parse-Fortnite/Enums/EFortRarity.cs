using CUE4Parse.UE4.Objects.Core.i18N;

namespace CUE4Parse_Fortnite.Enums
{
    public enum EFortRarity
    {
        Uncommon, // Default
        
        Masterwork,
        Transcendent,

        Elegant,
        Mythic,

        Fine,
        Legendary,

        Quality,
        Epic,

        Sturdy,
        Rare,

        Handmade,
        Common
    }

    public static class RarityUtil
    {
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
            EFortRarity.Masterwork => _transcendent,
            EFortRarity.Transcendent => _transcendent,
            EFortRarity.Elegant => _mythic,
            EFortRarity.Mythic => _mythic,
            EFortRarity.Fine => _legendary,
            EFortRarity.Legendary => _legendary,
            EFortRarity.Quality => _epic,
            EFortRarity.Epic => _epic,
            EFortRarity.Sturdy => _rare,
            EFortRarity.Rare => _rare,
            EFortRarity.Handmade => _common,
            EFortRarity.Common => _common,
            _ => _uncommon
        };
    }
}