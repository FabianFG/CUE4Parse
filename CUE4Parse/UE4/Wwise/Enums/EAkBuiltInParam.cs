namespace CUE4Parse.UE4.Wwise.Enums;

public enum EAkBuiltInParam : byte
{
    None = 0x0,
    Start = 0x1,
#pragma warning disable CA1069
    Distance = 0x1,
#pragma warning restore CA1069
    Azimuth = 0x2,
    Elevation = 0x3,
    EmitterCone = 0x4,
    Obstruction = 0x5,
    Occlusion = 0x6,
    ListenerCone = 0x7,
    Diffraction = 0x8,
    TransmissionLoss = 0x9,
    Max = 0xA,
};

