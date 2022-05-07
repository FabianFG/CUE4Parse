using System;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Objects.Engine.Curves;
using CUE4Parse.Utils;
using Newtonsoft.Json;
using static CUE4Parse.UE4.Objects.Engine.Curves.ERichCurveCompressionFormat;
using static CUE4Parse.UE4.Objects.Engine.Curves.ERichCurveInterpMode;
using static CUE4Parse.UE4.Objects.Engine.Curves.ERichCurveTangentMode;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    [JsonConverter(typeof(FCurveDescConverter))]
    public /*private*/ struct FCurveDesc
    {
        public ERichCurveCompressionFormat CompressionFormat;
        public ERichCurveKeyTimeCompressionFormat KeyTimeCompressionFormat;
        public ERichCurveExtrapolation PreInfinityExtrap;
        public ERichCurveExtrapolation PostInfinityExtrap;
        public int NumKeys; // union { float ConstantValue; int NumKeys; }
        public int KeyDataOffset;

        public float ConstantValue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => BitConverter.Int32BitsToSingle(NumKeys);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => NumKeys = BitConverter.SingleToInt32Bits(value);
        }
    }

    public class FCurveDescConverter : JsonConverter<FCurveDesc>
    {
        public override void WriteJson(JsonWriter writer, FCurveDesc value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("CompressionFormat");
            writer.WriteValue(value.CompressionFormat.ToString());

            writer.WritePropertyName("KeyTimeCompressionFormat");
            writer.WriteValue(value.KeyTimeCompressionFormat.ToString());

            writer.WritePropertyName("PreInfinityExtrap");
            writer.WriteValue(value.PreInfinityExtrap.ToString());

            writer.WritePropertyName("PostInfinityExtrap");
            writer.WriteValue(value.PostInfinityExtrap.ToString());

            if (value.CompressionFormat == RCCF_Constant)
            {
                writer.WritePropertyName("ConstantValue");
                writer.WriteValue(value.ConstantValue);
            }
            else
            {
                writer.WritePropertyName("NumKeys");
                writer.WriteValue(value.NumKeys);
            }

            writer.WritePropertyName("KeyDataOffset");
            writer.WriteValue(value.KeyDataOffset);

            writer.WriteEndObject();
        }

        public override FCurveDesc ReadJson(JsonReader reader, Type objectType, FCurveDesc existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public class UAnimCurveCompressionCodec_CompressedRichCurve : UAnimCurveCompressionCodec
    {
        private unsafe delegate FRichCurve CompressedCurveConverter(ERichCurveExtrapolation preInfinityExtrap, ERichCurveExtrapolation postInfinityExtrap, int constantValueNumKeys, byte* compressedKeys);

        private static readonly unsafe CompressedCurveConverter[][] ConverterMap =
        {
            // RCCF_Empty
            new CompressedCurveConverter[]
            {
                (preInfinityExtrap, postInfinityExtrap, constantValue, _) => new FRichCurve
                {
                    DefaultValue = *(float*) &constantValue,
                    PreInfinityExtrap = preInfinityExtrap,
                    PostInfinityExtrap = postInfinityExtrap,
                    Keys = Array.Empty<FRichCurveKey>()
                },
                (preInfinityExtrap, postInfinityExtrap, constantValue, _) => new FRichCurve
                {
                    DefaultValue = *(float*) &constantValue,
                    PreInfinityExtrap = preInfinityExtrap,
                    PostInfinityExtrap = postInfinityExtrap,
                    Keys = Array.Empty<FRichCurveKey>()
                }
            },
            // RCCF_Constant
            new CompressedCurveConverter[]
            {
                (preInfinityExtrap, postInfinityExtrap, constantValue, _) => new FRichCurve
                {
                    DefaultValue = 3.402823466e+38f, // MAX_flt
                    PreInfinityExtrap = preInfinityExtrap,
                    PostInfinityExtrap = postInfinityExtrap,
                    Keys = new FRichCurveKey[] { new(0.0f, *(float*) &constantValue) }
                },
                (preInfinityExtrap, postInfinityExtrap, constantValue, _) => new FRichCurve
                {
                    DefaultValue = 3.402823466e+38f, // MAX_flt
                    PreInfinityExtrap = preInfinityExtrap,
                    PostInfinityExtrap = postInfinityExtrap,
                    Keys = new FRichCurveKey[] { new(0.0f, *(float*) &constantValue) }
                }
            },
            // RCCF_Linear
            new CompressedCurveConverter[]
            {
                // RCKTCF_uint16
                (preInfinityExtrap, postInfinityExtrap, numKeys, compressedKeys) =>
                {
                    var keyTimesOffset = 0;
                    var keyTimeAdapter = new Quantized16BitKeyTimeAdapter(compressedKeys, keyTimesOffset, numKeys);
                    var keyDataAdapter = new UniformKeyDataAdapter(RCCF_Linear, compressedKeys, keyTimeAdapter);
                    return ConvertToRaw(keyTimeAdapter, keyDataAdapter, numKeys, preInfinityExtrap, postInfinityExtrap);
                },
                // RCKTCF_float32
                (preInfinityExtrap, postInfinityExtrap, numKeys, compressedKeys) =>
                {
                    var keyTimesOffset = 0;
                    var keyTimeAdapter = new Float32BitKeyTimeAdapter(compressedKeys, keyTimesOffset, numKeys);
                    var keyDataAdapter = new UniformKeyDataAdapter(RCCF_Linear, compressedKeys, keyTimeAdapter);
                    return ConvertToRaw(keyTimeAdapter, keyDataAdapter, numKeys, preInfinityExtrap, postInfinityExtrap);
                }
            },
            // RCCF_Cubic
            new CompressedCurveConverter[]
            {
                // RCKTCF_uint16
                (preInfinityExtrap, postInfinityExtrap, numKeys, compressedKeys) =>
                {
                    var keyTimesOffset = 0;
                    var keyTimeAdapter = new Quantized16BitKeyTimeAdapter(compressedKeys, keyTimesOffset, numKeys);
                    var keyDataAdapter = new UniformKeyDataAdapter(RCCF_Cubic, compressedKeys, keyTimeAdapter);
                    return ConvertToRaw(keyTimeAdapter, keyDataAdapter, numKeys, preInfinityExtrap, postInfinityExtrap);
                },
                // RCKTCF_float32
                (preInfinityExtrap, postInfinityExtrap, numKeys, compressedKeys) =>
                {
                    var keyTimesOffset = 0;
                    var keyTimeAdapter = new Float32BitKeyTimeAdapter(compressedKeys, keyTimesOffset, numKeys);
                    var keyDataAdapter = new UniformKeyDataAdapter(RCCF_Cubic, compressedKeys, keyTimeAdapter);
                    return ConvertToRaw(keyTimeAdapter, keyDataAdapter, numKeys, preInfinityExtrap, postInfinityExtrap);
                }
            },
            // RCCF_Mixed
            new CompressedCurveConverter[]
            {
                // RCKTCF_uint16
                (preInfinityExtrap, postInfinityExtrap, numKeys, compressedKeys) =>
                {
                    var interpModesOffset = 0;
                    var keyTimesOffset = interpModesOffset + (numKeys * sizeof(byte)).Align(sizeof(ushort));
                    var keyTimeAdapter = new Quantized16BitKeyTimeAdapter(compressedKeys, keyTimesOffset, numKeys);
                    var keyDataAdapter = new MixedKeyDataAdapter(compressedKeys, interpModesOffset, keyTimeAdapter);
                    return ConvertToRaw(keyTimeAdapter, keyDataAdapter, numKeys, preInfinityExtrap, postInfinityExtrap);
                },
                // RCKTCF_float32
                (preInfinityExtrap, postInfinityExtrap, numKeys, compressedKeys) =>
                {
                    var interpModesOffset = 0;
                    var keyTimesOffset = interpModesOffset + (numKeys * sizeof(byte)).Align(sizeof(float));
                    var keyTimeAdapter = new Float32BitKeyTimeAdapter(compressedKeys, keyTimesOffset, numKeys);
                    var keyDataAdapter = new MixedKeyDataAdapter(compressedKeys, interpModesOffset, keyTimeAdapter);
                    return ConvertToRaw(keyTimeAdapter, keyDataAdapter, numKeys, preInfinityExtrap, postInfinityExtrap);
                }
            },
            // RCCF_Weighted
            new CompressedCurveConverter[]
            {
                // RCKTCF_uint16
                (preInfinityExtrap, postInfinityExtrap, numKeys, compressedKeys) =>
                {
                    var interpModesOffset = 0;
                    var keyTimesOffset = interpModesOffset + (2 * numKeys * sizeof(byte)).Align(sizeof(ushort));
                    var keyTimeAdapter = new Quantized16BitKeyTimeAdapter(compressedKeys, keyTimesOffset, numKeys);
                    var keyDataAdapter = new WeightedKeyDataAdapter(compressedKeys, interpModesOffset, keyTimeAdapter);
                    return ConvertToRaw(keyTimeAdapter, keyDataAdapter, numKeys, preInfinityExtrap, postInfinityExtrap);
                },
                // RCKTCF_float32
                (preInfinityExtrap, postInfinityExtrap, numKeys, compressedKeys) =>
                {
                    var interpModesOffset = 0;
                    var keyTimesOffset = interpModesOffset + (2 * numKeys * sizeof(byte)).Align(sizeof(float));
                    var keyTimeAdapter = new Float32BitKeyTimeAdapter(compressedKeys, keyTimesOffset, numKeys);
                    var keyDataAdapter = new WeightedKeyDataAdapter(compressedKeys, interpModesOffset, keyTimeAdapter);
                    return ConvertToRaw(keyTimeAdapter, keyDataAdapter, numKeys, preInfinityExtrap, postInfinityExtrap);
                }
            },
        };

        private static FRichCurve ConvertToRaw(IKeyTimeAdapter keyTimeAdapter, IKeyDataAdapter keyDataAdapter, int numKeys, ERichCurveExtrapolation preInfinityExtrap, ERichCurveExtrapolation postInfinityExtrap)
        {
            var curve = new FRichCurve();
            curve.DefaultValue = 3.402823466e+38f;
            curve.PreInfinityExtrap = preInfinityExtrap;
            curve.PostInfinityExtrap = postInfinityExtrap;
            curve.Keys = new FRichCurveKey[numKeys];
            for (var keyIndex = 0; keyIndex < numKeys; keyIndex++)
            {
                var handle = keyDataAdapter.GetKeyDataHandle(keyIndex);
                var interpMode = keyDataAdapter.GetKeyInterpMode(keyIndex);
                var key = new FRichCurveKey();
                key.InterpMode = interpMode switch
                {
                    RCCF_Linear => RCIM_Linear,
                    RCCF_Cubic => RCIM_Cubic,
                    RCCF_Constant => RCIM_Constant,
                    _ => throw new ArgumentException("Can't convert interpMode " + interpMode + " to ERichCurveInterpMode")
                };
                key.TangentMode = RCTM_Auto; // How to convert? interpMode == RCCF_Weighted && keyDataAdapter.GetKeyTangentWeightMode(keyIndex) != RCTWM_WeightedNone ? RCTM_User : RCTM_Auto;
                key.TangentWeightMode = keyDataAdapter.GetKeyTangentWeightMode(keyIndex);
                key.Time = keyTimeAdapter.GetTime(keyIndex);
                key.Value = keyDataAdapter.GetKeyValue(handle);
                key.ArriveTangent = keyDataAdapter.GetKeyArriveTangent(handle);
                key.ArriveTangentWeight = keyDataAdapter.GetKeyArriveTangentWeight(handle);
                key.LeaveTangent = keyDataAdapter.GetKeyLeaveTangent(handle);
                key.LeaveTangentWeight = keyDataAdapter.GetKeyLeaveTangentWeight(handle);
                curve.Keys[keyIndex] = key;
            }
            return curve;
        }

        public override unsafe FFloatCurve[] ConvertCurves(UAnimSequence animSeq)
        {
            if (animSeq.CompressedCurveByteStream == null || animSeq.CompressedCurveByteStream.Length == 0)
            {
                return Array.Empty<FFloatCurve>();
            }

            fixed (byte* buffer = &animSeq.CompressedCurveByteStream[0])
            {
                var curveDescriptions = (FCurveDesc*) buffer;

                var compressedCurveNames = animSeq.CompressedCurveNames;
                var numCurves = compressedCurveNames.Length;
                var floatCurves = new FFloatCurve[numCurves];
                for (var curveIndex = 0; curveIndex < numCurves; ++curveIndex)
                {
                    var curveName = compressedCurveNames[curveIndex];
                    var curve = curveDescriptions[curveIndex];
                    var compressedKeys = buffer + curve.KeyDataOffset;
                    var rawCurve = ConverterMap[(int) curve.CompressionFormat][(int) curve.KeyTimeCompressionFormat](curve.PreInfinityExtrap, curve.PostInfinityExtrap, curve.NumKeys, compressedKeys);
                    floatCurves[curveIndex] = new FFloatCurve
                    {
                        Name = curveName,
                        FloatCurve = rawCurve,
                        CurveTypeFlags = 4
                    };
                }
                return floatCurves;
            }
        }
    }
}
