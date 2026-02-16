using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkAcousticTexture(FArchive Ar) : ICAkIndexable
{
    public readonly uint Id = Ar.Read<uint>();
    public readonly float AbsorptionOffset = Ar.Read<float>();
    public readonly float AbsorptionLow = Ar.Read<float>();
    public readonly float AbsorptionMidLow = Ar.Read<float>();
    public readonly float AbsorptionMidHigh = Ar.Read<float>();
    public readonly float AbsorptionHigh = Ar.Read<float>();
    public readonly float Scattering = Ar.Read<float>();
}

// > 118 <= 122
public readonly struct AkAcousticTexture_v122(FArchive Ar) : ICAkIndexable
{
    public readonly uint Id = Ar.Read<uint>();

    public readonly bool OnOffBand1 = Ar.Read<ushort>() != 0;
    public readonly bool OnOffBand2 = Ar.Read<ushort>() != 0;
    public readonly bool OnOffBand3 = Ar.Read<ushort>() != 0;

    public readonly ushort FilterTypeBand1 = Ar.Read<ushort>();
    public readonly ushort FilterTypeBand2 = Ar.Read<ushort>();
    public readonly ushort FilterTypeBand3 = Ar.Read<ushort>();

    public readonly float FrequencyBand1 = Ar.Read<float>();
    public readonly float FrequencyBand2 = Ar.Read<float>();
    public readonly float FrequencyBand3 = Ar.Read<float>();

    public readonly float QFactorBand1 = Ar.Read<float>();
    public readonly float QFactorBand2 = Ar.Read<float>();
    public readonly float QFactorBand3 = Ar.Read<float>();

    public readonly float GainBand1 = Ar.Read<float>();
    public readonly float GainBand2 = Ar.Read<float>();
    public readonly float GainBand3 = Ar.Read<float>();

    public readonly float OutputGain = Ar.Read<float>();
}
