namespace CUE4Parse.UE4.Assets.Exports;

[Flags]
public enum EObjectFlagsLegacy : ulong
{
    None = 0,

    Transactional = 0x00000001UL,
    InSingularFunc = 0x00000002UL,

    Public = 0x00000004UL << 32,

    Private = 0x00000080UL,
    Final = 0x00000080UL,

    Automated = 0x00000100UL,
    PerObjectLocalized = 0x00000100UL,

    Protected = 0x00000800UL,
    PropertiesObject = 0x00000200UL,
    ArchetypeObject = 0x00000400UL,

    Transient = 0x00004000UL << 32,

    LoadForClient = 0x00010000UL << 32,
    LoadForServer = 0x00020000UL << 32,
    LoadForEdit = 0x00040000UL << 32,
    Standalone = 0x00080000UL << 32,

    NotForClient = 0x00100000UL << 32,
    NotForServer = 0x00200000UL << 32,
    NotForEdit = 0x00400000UL << 32,

    HasStack = 0x02000000UL << 32,
    Native = 0x04000000UL << 32,

    Marked = 0x08000000UL,

    TemplateObject = PropertiesObject | ArchetypeObject,
}
