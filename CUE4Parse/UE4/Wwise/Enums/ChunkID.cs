namespace CUE4Parse.UE4.Wwise.Enums;

public enum ChunkID : uint
{
    AKPK = 0x4B504B41, // Audiokinetic Bank
    BankHeader = 0x44484B42, // Bank Header
    BankInit = 0x54494E49, // Plugin
    BankDataIndex = 0x58444944, // Media Index
    BankData = 0x41544144,
    BankHierarchy = 0x43524948, // Hierarchy
    RIFF = 0x46464952,
    BankStrMap = 0x44495453, // String Mappings
    BankStateMg = 0x474D5453, // Global Settings
    BankEnvSetting = 0x53564E45, // Environment Settings
    FXPR = 0x52505846, // FX Parameters
    BankCustomPlatformName = 0x54414C50, // Custom Platform
    PLUGIN = 0x47554C50,
}
