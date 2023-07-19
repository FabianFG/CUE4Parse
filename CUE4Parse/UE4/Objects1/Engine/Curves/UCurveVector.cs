using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Engine.Curves
{
    public class UCurveVector : Assets.Exports.UObject
    {
        public readonly FRichCurve[] FloatCurves = new FRichCurve[3];

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            for (var i = 0; i < Properties.Count; ++i)
            {
                if (Properties[i].Tag?.GenericValue is UScriptStruct { StructType: FStructFallback fallback })
                {
                    FloatCurves[i] = new FRichCurve(fallback);
                }
            }

            if (FloatCurves.Length > 0) Properties.Clear(); // Don't write these for this object
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("FloatCurves");
            writer.WriteStartArray();

            foreach (var richCurve in FloatCurves)
            {
                serializer.Serialize(writer, richCurve);
            }

            writer.WriteEndArray();
        }
    }
}
