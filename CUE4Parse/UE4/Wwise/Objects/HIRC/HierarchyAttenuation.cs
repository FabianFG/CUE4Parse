using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class HierarchyAttenuation : AbstractHierarchy
{
    public byte IsHeightSpreadEnabled { get; private set; }
    public byte IsConeEnabled { get; private set; }

    public float? InsideDegrees { get; private set; }
    public float? OutsideDegrees { get; private set; }
    public float? OutsideVolume { get; private set; }
    public float? LoPass { get; private set; }
    public float? HiPass { get; private set; }

    public List<AkConversionTable> Curves { get; }

    public List<AkRTPC> RTPCs { get; protected set; }

    public HierarchyAttenuation(FArchive Ar) : base(Ar)
    {
        if (WwiseVersions.WwiseVersion > 136)
        {
            IsHeightSpreadEnabled = Ar.Read<byte>();
        }

        IsConeEnabled = Ar.Read<byte>();
        if ((IsConeEnabled & 1) != 0)
        {
            InsideDegrees = Ar.Read<float>();
            OutsideDegrees = Ar.Read<float>();
            OutsideVolume = Ar.Read<float>();
            LoPass = Ar.Read<float>();

            if (WwiseVersions.WwiseVersion > 89)
            {
                HiPass = Ar.Read<float>();
            }
        }

        int numCurves = WwiseVersions.WwiseVersion switch
        {
            <= 62 => 5,
            <= 72 => 4,
            <= 89 => 5,
            <= 141 => 7,
            _ => 19
        };

        for (int i = 0; i < numCurves; i++)
        {
            sbyte curveToUse = Ar.Read<sbyte>();
        }

        int numCurvesFinal;
        if (WwiseVersions.WwiseVersion <= 36)
        {
            numCurvesFinal = (int) Ar.Read<uint>(); // Use uint for legacy versions and cast to int
        }
        else
        {
            numCurvesFinal = Ar.Read<byte>(); // Use byte for modern versions
        }

        Curves = [];
        for (int i = 0; i < numCurvesFinal; i++)
        {
            Curves.Add(new AkConversionTable(Ar));
        }

        RTPCs = AkRTPC.ReadMultiple(Ar);
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("IsHeightSpreadEnabled");
        writer.WriteValue(IsHeightSpreadEnabled != 0);

        writer.WritePropertyName("IsConeEnabled");
        writer.WriteValue((IsConeEnabled & 1) != 0);

        if ((IsConeEnabled & 1) != 0)
        {
            writer.WritePropertyName("InsideDegrees");
            writer.WriteValue(InsideDegrees);

            writer.WritePropertyName("OutsideDegrees");
            writer.WriteValue(OutsideDegrees);

            writer.WritePropertyName("OutsideVolume");
            writer.WriteValue(OutsideVolume);

            writer.WritePropertyName("LoPass");
            writer.WriteValue(LoPass);

            if (HiPass.HasValue)
            {
                writer.WritePropertyName("HiPass");
                writer.WriteValue(HiPass);
            }
        }

        writer.WritePropertyName("Curves");
        serializer.Serialize(writer, Curves);

        writer.WritePropertyName("RTPCs");
        serializer.Serialize(writer, RTPCs);

        writer.WriteEndObject();
    }
}
