using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;

// CAkAttenuation
public class HierarchyAttenuation : AbstractHierarchy
{
    public readonly bool IsHeightSpreadEnabled;
    public readonly bool IsConeEnabled;
    public readonly ConeParams? ConeParams;
    public readonly CAkConversionTable[] Curves;
    public readonly AkRtpc[] RTPCs;

    // CAkAttenuation::SetInitialValues
    public HierarchyAttenuation(FArchive Ar) : base(Ar)
    {
        if (WwiseVersions.Version > 136)
        {
            IsHeightSpreadEnabled = Ar.Read<byte>() != 0;
        }

        IsConeEnabled = (Ar.Read<byte>() & 1) != 0;
        if (IsConeEnabled)
        {
            ConeParams = new ConeParams(Ar);
        }

        int numCurves = WwiseVersions.Version switch
        {
            <= 62 => 5,
            <= 72 => 4,
            <= 89 => 5,
            <= 141 => 7,
            <= 154 => 19,
            _ => 24
        };

        var curvesToUse = Ar.ReadArray<sbyte>(numCurves);

        int numCurvesFinal;
        if (WwiseVersions.Version <= 36)
        {
            numCurvesFinal = (int) Ar.Read<uint>(); // Use uint for legacy versions and cast to int
        }
        else
        {
            numCurvesFinal = Ar.Read<byte>(); // Use byte for modern versions
        }

        Curves = Ar.ReadArray(numCurvesFinal, () => new CAkConversionTable(Ar));
        RTPCs = AkRtpc.ReadArray(Ar);
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(nameof(IsHeightSpreadEnabled));
        writer.WriteValue(IsHeightSpreadEnabled);

        writer.WritePropertyName(nameof(IsConeEnabled));
        writer.WriteValue(IsConeEnabled);

        if (IsConeEnabled)
        {
            writer.WritePropertyName(nameof(ConeParams));
            serializer.Serialize(writer, ConeParams);
        }

        writer.WritePropertyName(nameof(Curves));
        serializer.Serialize(writer, Curves);

        writer.WritePropertyName(nameof(RTPCs));
        serializer.Serialize(writer, RTPCs);

        writer.WriteEndObject();
    }
}

public class ConeParams
{
    public float fInsideDegrees;
    public float fOutsideDegrees;
    public float fOutsideVolume;
    public float LoPass;
    public float HiPass;

    public ConeParams(FArchive Ar)
    {
        fInsideDegrees = Ar.Read<float>();
        fOutsideDegrees = Ar.Read<float>();
        fOutsideVolume = Ar.Read<float>();
        LoPass = Ar.Read<float>();
        if (WwiseVersions.Version > 89)
        {
            HiPass = Ar.Read<float>();
        }
    }
}