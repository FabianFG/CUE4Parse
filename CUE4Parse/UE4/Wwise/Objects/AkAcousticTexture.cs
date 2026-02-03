using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkAcousticTexture
{
    public readonly uint Id;
    public readonly float AbsorptionOffset;
    public readonly float AbsorptionLow;
    public readonly float AbsorptionMidLow;
    public readonly float AbsorptionMidHigh;
    public readonly float AbsorptionHigh;
    public readonly float Scattering;

    public AkAcousticTexture(FArchive Ar)
    {
        Id = Ar.Read<uint>();
        AbsorptionOffset = Ar.Read<float>();
        AbsorptionLow = Ar.Read<float>();
        AbsorptionMidLow = Ar.Read<float>();
        AbsorptionMidHigh = Ar.Read<float>();
        AbsorptionHigh = Ar.Read<float>();
        Scattering = Ar.Read<float>();
    }
}

// > 118 <= 122
public readonly struct AkAcousticTexture_v122
{
    public readonly uint Id;

    public readonly ushort OnOffBand1;
    public readonly ushort OnOffBand2;
    public readonly ushort OnOffBand3;

    public readonly ushort FilterTypeBand1;
    public readonly ushort FilterTypeBand2;
    public readonly ushort FilterTypeBand3;

    public readonly float FrequencyBand1;
    public readonly float FrequencyBand2;
    public readonly float FrequencyBand3;

    public readonly float QFactorBand1;
    public readonly float QFactorBand2;
    public readonly float QFactorBand3;

    public readonly float GainBand1;
    public readonly float GainBand2;
    public readonly float GainBand3;

    public readonly float OutputGain;

    public AkAcousticTexture_v122(FArchive Ar)
    {
        Id = Ar.Read<uint>();

        OnOffBand1 = Ar.Read<ushort>();
        OnOffBand2 = Ar.Read<ushort>();
        OnOffBand3 = Ar.Read<ushort>();

        FilterTypeBand1 = Ar.Read<ushort>();
        FilterTypeBand2 = Ar.Read<ushort>();
        FilterTypeBand3 = Ar.Read<ushort>();

        FrequencyBand1 = Ar.Read<float>();
        FrequencyBand2 = Ar.Read<float>();
        FrequencyBand3 = Ar.Read<float>();

        QFactorBand1 = Ar.Read<float>();
        QFactorBand2 = Ar.Read<float>();
        QFactorBand3 = Ar.Read<float>();

        GainBand1 = Ar.Read<float>();
        GainBand2 = Ar.Read<float>();
        GainBand3 = Ar.Read<float>();

        OutputGain = Ar.Read<float>();
    }
}
