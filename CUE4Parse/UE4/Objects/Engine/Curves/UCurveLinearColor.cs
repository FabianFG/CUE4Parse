using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Engine.Curves
{
    public class UCurveLinearColor : Assets.Exports.UObject
    {
        public readonly FRichCurve[] FloatCurves = new FRichCurve[4];
        private float AdjustBrightness, AdjustBrightnessCurve, AdjustVibrance, AdjustSaturation, AdjustHue, AdjustMinAlpha, AdjustMaxAlpha;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            AdjustBrightness = GetOrDefault<float>(nameof(AdjustBrightness));
            AdjustBrightnessCurve = GetOrDefault<float>(nameof(AdjustBrightnessCurve));
            AdjustVibrance = GetOrDefault<float>(nameof(AdjustVibrance));
            AdjustSaturation = GetOrDefault<float>(nameof(AdjustSaturation));
            AdjustHue = GetOrDefault<float>(nameof(AdjustHue));
            AdjustMinAlpha = GetOrDefault<float>(nameof(AdjustMinAlpha));
            AdjustMaxAlpha = GetOrDefault<float>(nameof(AdjustMaxAlpha));

            for (var i = 0; i < Properties.Count; ++i)
            {
                if (Properties[i].Tag?.GenericValue is FScriptStruct { StructType: FStructFallback fallback })
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

        public FLinearColor GetUnadjustedLinearColorValue(float inTime)
        {
            return new FLinearColor(FloatCurves[0].Eval(inTime), FloatCurves[1].Eval(inTime), FloatCurves[2].Eval(inTime), FloatCurves[3].Keys.Length == 0 ? 1.0f : FloatCurves[3].Eval(inTime));
        }

        public FLinearColor GetLinearColorValue(float inTime)
        {
            var originalColor = GetUnadjustedLinearColorValue(inTime);

            var bShouldClampValue = originalColor.R <= 1.0f && originalColor.G <= 1.0f && originalColor.B <= 1.0f;

            var hsvColor = originalColor.LinearRGBToHsv();
            var pixelHue = hsvColor.R;
            var pixelSaturation = hsvColor.G;
            var pixelValue = hsvColor.B;

            pixelValue *= AdjustBrightness;

            if (!UnrealMath.IsNearlyEqual(AdjustBrightnessCurve, 1.0f, UnrealMath.KindaSmallNumber) && AdjustBrightnessCurve != 0.0f)
            {
                // Raise HSV.V to the specified power
                pixelValue = (float) Math.Pow(pixelValue, AdjustBrightnessCurve);
            }

            // Apply "vibrancy" adjustment
            if (!UnrealMath.IsNearlyZero(AdjustBrightness))
            {
                var invSatRaised = Math.Pow(1.0f - pixelSaturation, 5.0f);
                var clampedVibrance = Math.Clamp(AdjustVibrance, 0.0f, 1.0f);
                var halfVibrance = clampedVibrance * 0.5f;
                var satProduct = halfVibrance * invSatRaised;

                pixelSaturation += (float) satProduct;
            }

            // Apply saturation adjustment
            pixelSaturation *= AdjustSaturation;

            // Apply hue adjustment
            pixelHue += AdjustHue;

            // Clamp HSV values
            {
                pixelHue = UnrealMath.Fmod(pixelHue, 360.0f);
                if (pixelHue < 0.0f)
                {
                    // Keep the hue value positive as HSVToLinearRGB prefers that
                    pixelHue += 360.0f;
                }

                pixelSaturation = Math.Clamp(pixelSaturation, 0.0f, 1.0f);

                if (bShouldClampValue)
                {
                    pixelValue = Math.Clamp(pixelValue, 0.0f, 1.0f);
                }
            }

            var linearColor = hsvColor.HSVToLinearRGB();
            linearColor.A = MathUtils.Lerp(AdjustMinAlpha, AdjustMaxAlpha, originalColor.A);
            return linearColor;
        }
    }
}
