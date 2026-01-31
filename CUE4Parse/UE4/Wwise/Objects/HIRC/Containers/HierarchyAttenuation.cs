using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;

// CAkAttenuation
public class HierarchyAttenuation : AbstractHierarchy
{
    public readonly bool IsHeightSpreadEnabled;
    public readonly bool IsConeEnabled;
    public readonly float? InsideDegrees;
    public readonly float? OutsideDegrees;
    public readonly float? OutsideVolume;
    public readonly float? LoPass;
    public readonly float? HiPass;
    public readonly AkConversionTable[] Curves;
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
            InsideDegrees = Ar.Read<float>();
            OutsideDegrees = Ar.Read<float>();
            OutsideVolume = Ar.Read<float>();
            LoPass = Ar.Read<float>();

            if (WwiseVersions.Version > 89)
            {
                HiPass = Ar.Read<float>();
            }
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

        for (int i = 0; i < numCurves; i++)
        {
            sbyte curveToUse = Ar.Read<sbyte>();
        }

        int numCurvesFinal;
        if (WwiseVersions.Version <= 36)
        {
            numCurvesFinal = (int) Ar.Read<uint>(); // Use uint for legacy versions and cast to int
        }
        else
        {
            numCurvesFinal = Ar.Read<byte>(); // Use byte for modern versions
        }

        Curves = Ar.ReadArray(numCurvesFinal, () => new AkConversionTable(Ar));
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
            writer.WritePropertyName(nameof(InsideDegrees));
            writer.WriteValue(InsideDegrees);

            writer.WritePropertyName(nameof(OutsideDegrees));
            writer.WriteValue(OutsideDegrees);

            writer.WritePropertyName(nameof(OutsideVolume));
            writer.WriteValue(OutsideVolume);

            writer.WritePropertyName(nameof(LoPass));
            writer.WriteValue(LoPass);

            if (HiPass.HasValue)
            {
                writer.WritePropertyName(nameof(HiPass));
                writer.WriteValue(HiPass);
            }
        }

        writer.WritePropertyName(nameof(Curves));
        serializer.Serialize(writer, Curves);

        writer.WritePropertyName(nameof(RTPCs));
        serializer.Serialize(writer, RTPCs);

        writer.WriteEndObject();
    }
}
