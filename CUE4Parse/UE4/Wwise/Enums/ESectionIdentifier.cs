namespace CUE4Parse.UE4.Wwise.Enums;

public enum ESectionIdentifier : uint
{
    AKPK = 0x4B504B41, // Audiokinetic Bank
    BKHD = 0x44484B42, // Bank Header
    INIT = 0x54494E49, // Plugin
    DIDX = 0x58444944, // Media Index
    DATA = 0x41544144,
    HIRC = 0x43524948, // Hierarchy
    RIFF = 0x46464952,
    STID = 0x44495453, // String Mappings
    STMG = 0x474D5453, // Global Settings
    ENVS = 0x53564E45, // Enviroment Settings
    FXPR = 0x52505846, // FX Parameters
    PLAT = 0x54414C50 // Custom Platform
}
