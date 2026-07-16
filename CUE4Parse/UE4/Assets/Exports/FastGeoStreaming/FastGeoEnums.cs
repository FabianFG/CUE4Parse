namespace CUE4Parse.UE4.Assets.Exports.FastGeoStreaming;

public enum ESkinCacheUsage : byte
{
    Auto = 0,
    Disabled = 255,
    Enabled = 1,
}


public enum EDetailMode : byte
{
    Low = 0,
    Medium = 1,
    High = 2,
    Epic = 3
}

public enum EHasCustomNavigableGeometry : byte
{
    No,
    Yes,
    EvenIfNotCollidable,
    DontExport
}

public enum EComponentMobility : byte
{
    Static,
    Stationary,
    Movable
}

public enum ELightmapType : byte
{
    Default,
    ForceSurface,
    ForceVolumetric
}

public enum ESceneDepthPriorityGroup : byte
{
    World,
    Foreground
}

public enum ERendererStencilMask : byte
{
    ERSM_Default,
    ERSM_255,
    ERSM_1,
    ERSM_2,
    ERSM_4,
    ERSM_8,
    ERSM_16,
    ERSM_32,
    ERSM_64,
    ERSM_128,
}

public enum ERayTracingGroupCullingPriority : byte
{
    CP_0_NEVER_CULL,
    CP_1,
    CP_2,
    CP_3,
    CP_4_DEFAULT,
    CP_5,
    CP_6,
    CP_7,
    CP_8_QUICKLY_CULL,
}

public enum EIndirectLightingCacheQuality : byte
{
    Off,
    Point,
    Volume
}

public enum EShadowCacheInvalidationBehavior : byte
{
    Auto,
    Always,
    Rigid,
    Static
}

public enum ERuntimeVirtualTextureMainPassType : byte
{
    Never,
    Exclusive,
    Always
}
