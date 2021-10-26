using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static CUE4Parse.UE4.Objects.Engine.Curves.ERichCurveCompressionFormat;
using static CUE4Parse.UE4.Objects.Engine.Curves.ERichCurveInterpMode;
using static CUE4Parse.UE4.Objects.Engine.Curves.ERichCurveTangentMode;
using static CUE4Parse.UE4.Objects.Engine.Curves.ERichCurveTangentWeightMode;

namespace CUE4Parse.UE4.Objects.Engine.Curves
{
    /** If using RCIM_Cubic, this enum describes how the tangents should be controlled in editor. */
    public enum ERichCurveTangentMode : byte
    {
        /** Automatically calculates tangents to create smooth curves between values. */
        RCTM_Auto,
        /** User specifies the tangent as a unified tangent where the two tangents are locked to each other, presenting a consistent curve before and after. */
        RCTM_User,
        /** User specifies the tangent as two separate broken tangents on each side of the key which can allow a sharp change in evaluation before or after. */
        RCTM_Break,
        /** No tangents. */
        RCTM_None
    }

    /** Enumerates tangent weight modes. */
    public enum ERichCurveTangentWeightMode : byte
    {
        /** Don't take tangent weights into account. */
        RCTWM_WeightedNone,
        /** Only take the arrival tangent weight into account for evaluation. */
        RCTWM_WeightedArrive,
        /** Only take the leaving tangent weight into account for evaluation. */
        RCTWM_WeightedLeave,
        /** Take both the arrival and leaving tangent weights into account for evaluation. */
        RCTWM_WeightedBoth
    }

    /** Enumerates curve compression options. */
    public enum ERichCurveCompressionFormat : byte
    {
        /** No keys are present */
        RCCF_Empty,

        /** All keys use constant interpolation */
        RCCF_Constant,

        /** All keys use linear interpolation */
        RCCF_Linear,

        /** All keys use cubic interpolation */
        RCCF_Cubic,

        /** Keys use mixed interpolation modes */
        RCCF_Mixed,

        /** Keys use weighted interpolation modes */
        RCCF_Weighted,
    }

    /** Enumerates key time compression options. */
    public enum ERichCurveKeyTimeCompressionFormat : byte
    {
        /** Key time is quantized to 16 bits */
        RCKTCF_uint16,

        /** Key time uses full precision */
        RCKTCF_float32,
    }

    /** One key in a rich, editable float curve */
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FRichCurveKey : IUStruct
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ERichCurveInterpMode InterpMode;
        [JsonConverter(typeof(StringEnumConverter))]
        public ERichCurveTangentMode TangentMode;
        [JsonConverter(typeof(StringEnumConverter))]
        public ERichCurveTangentWeightMode TangentWeightMode;
        public float Time;
        public float Value;
        public float ArriveTangent;
        public float ArriveTangentWeight;
        public float LeaveTangent;
        public float LeaveTangentWeight;

        public FRichCurveKey(float time, float value)
        {
            InterpMode = RCIM_Linear;
            TangentMode = RCTM_Auto;
            TangentWeightMode = RCTWM_WeightedNone;
            Time = time;
            Value = value;
            ArriveTangent = 0.0f;
            ArriveTangentWeight = 0.0f;
            LeaveTangent = 0.0f;
            LeaveTangentWeight = 0.0f;
        }

        public FRichCurveKey(float time, float value, float arriveTangent, float leaveTangent, ERichCurveInterpMode interpMode)
        {
            InterpMode = interpMode;
            TangentMode = RCTM_Auto;
            TangentWeightMode = RCTWM_WeightedNone;
            Time = time;
            Value = value;
            ArriveTangent = arriveTangent;
            ArriveTangentWeight = 0.0f;
            LeaveTangent = leaveTangent;
            LeaveTangentWeight = 0.0f;
        }
    }

    /** A rich, editable float curve */
    public class FRichCurve : FRealCurve
    {
        public FRichCurveKey[] Keys;

        public FRichCurve() { }

        public FRichCurve(FStructFallback data)
        {
            Keys = data.GetOrDefault<FRichCurveKey[]>(nameof(Keys));
        }
    }

    internal interface IKeyTimeAdapter
    {
        public int KeyDataOffset { get; }
        public float GetTime(int keyIndex);
    }

    internal interface IKeyDataAdapter
    {
        public int GetKeyDataHandle(int keyIndexToQuery);
        public float GetKeyValue(int handle);
        public float GetKeyArriveTangent(int handle);
        public float GetKeyLeaveTangent(int handle);
        public ERichCurveCompressionFormat GetKeyInterpMode(int keyIndex);
        public ERichCurveTangentWeightMode GetKeyTangentWeightMode(int keyIndex);
        public float GetKeyArriveTangentWeight(int handle);
        public float GetKeyLeaveTangentWeight(int handle);
    }

    internal readonly unsafe struct Quantized16BitKeyTimeAdapter : IKeyTimeAdapter
    {
        public const float QuantizationScale = 1.0f / 65535.0f;
        public const int KeySize = sizeof(ushort);
        public const int RangeDataSize = 2 * sizeof(float);

        public readonly ushort* KeyTimes;
        public readonly float MinTime;
        public readonly float DeltaTime;
        public int KeyDataOffset { get; }

        public Quantized16BitKeyTimeAdapter(byte* basePtr, int keyTimesOffset, int numKeys)
        {
            var rangeDataOffset = (keyTimesOffset + (numKeys * sizeof(ushort))).Align(sizeof(float));
            KeyDataOffset = rangeDataOffset + RangeDataSize;
            var rangeData = (float*) (basePtr + rangeDataOffset);

            KeyTimes = (ushort*) (basePtr + keyTimesOffset);
            MinTime = rangeData[0];
            DeltaTime = rangeData[1];
        }

        public float GetTime(int keyIndex)
        {
            var keyNormalizedTime = KeyTimes[keyIndex] * QuantizationScale;
            return (keyNormalizedTime * DeltaTime) + MinTime;
        }
    }

    internal readonly unsafe struct Float32BitKeyTimeAdapter : IKeyTimeAdapter
    {
        public const int KeySize = sizeof(float);
        public const int RangeDataSize = 0;

        public readonly float* KeyTimes;
        public int KeyDataOffset { get; }

        public Float32BitKeyTimeAdapter(byte* basePtr, int keyTimesOffset, int numKeys)
        {
            KeyTimes = (float*) (basePtr + keyTimesOffset);
            KeyDataOffset = (keyTimesOffset + (numKeys * sizeof(float))).Align(sizeof(float));
        }

        public float GetTime(int keyIndex) => KeyTimes[keyIndex];
    }

    internal readonly unsafe struct UniformKeyDataAdapter : IKeyDataAdapter
    {
        private readonly ERichCurveCompressionFormat _format;
        private readonly float* _keyData;

        public UniformKeyDataAdapter(ERichCurveCompressionFormat format, byte* basePtr, IKeyTimeAdapter keyTimeAdapter)
        {
            _format = format;
            _keyData = (float*) (basePtr + keyTimeAdapter.KeyDataOffset);
        }

        public int GetKeyDataHandle(int keyIndexToQuery) => _format == RCCF_Cubic ? (keyIndexToQuery * 3) : keyIndexToQuery;
        public float GetKeyValue(int handle) => _keyData[handle];
        public float GetKeyArriveTangent(int handle) => _keyData[handle + 1];
        public float GetKeyLeaveTangent(int handle) => _keyData[handle + 2];
        public ERichCurveCompressionFormat GetKeyInterpMode(int keyIndex) => _format;
        public ERichCurveTangentWeightMode GetKeyTangentWeightMode(int keyIndex) => RCTWM_WeightedNone;
        public float GetKeyArriveTangentWeight(int handle) => 0.0f;
        public float GetKeyLeaveTangentWeight(int handle) => 0.0f;
    }

    internal readonly unsafe struct MixedKeyDataAdapter : IKeyDataAdapter
    {
        private readonly byte* _interpModes;
        private readonly float* _keyData;

        public MixedKeyDataAdapter(byte* basePtr, int interpModesOffset, IKeyTimeAdapter keyTimeAdapter)
        {
            _interpModes = basePtr + interpModesOffset;
            _keyData = (float*) (basePtr + keyTimeAdapter.KeyDataOffset);
        }

        public int GetKeyDataHandle(int keyIndexToQuery) => keyIndexToQuery * 3;
        public float GetKeyValue(int handle) => _keyData[handle];
        public float GetKeyArriveTangent(int handle) => _keyData[handle + 1];
        public float GetKeyLeaveTangent(int handle) => _keyData[handle + 2];
        public ERichCurveCompressionFormat GetKeyInterpMode(int keyIndex) => (ERichCurveCompressionFormat) _interpModes[keyIndex];
        public ERichCurveTangentWeightMode GetKeyTangentWeightMode(int keyIndex) => RCTWM_WeightedNone;
        public float GetKeyArriveTangentWeight(int handle) => 0.0f;
        public float GetKeyLeaveTangentWeight(int handle) => 0.0f;
    }

    internal readonly unsafe struct WeightedKeyDataAdapter : IKeyDataAdapter
    {
        private readonly byte* _interpModes;
        private readonly float* _keyData;

        public WeightedKeyDataAdapter(byte* basePtr, int interpModesOffset, IKeyTimeAdapter keyTimeAdapter)
        {
            _interpModes = basePtr + interpModesOffset;
            _keyData = (float*) (basePtr + keyTimeAdapter.KeyDataOffset);
        }

        public int GetKeyDataHandle(int keyIndexToQuery) => keyIndexToQuery * 3;
        public float GetKeyValue(int handle) => _keyData[handle];
        public float GetKeyArriveTangent(int handle) => _keyData[handle + 1];
        public float GetKeyLeaveTangent(int handle) => _keyData[handle + 2];
        public float GetKeyArriveTangentWeight(int handle) => _keyData[handle + 3];
        public float GetKeyLeaveTangentWeight(int handle) => _keyData[handle + 4];
        public ERichCurveCompressionFormat GetKeyInterpMode(int keyIndex) => (ERichCurveCompressionFormat) _interpModes[keyIndex];
        public ERichCurveTangentWeightMode GetKeyTangentWeightMode(int keyIndex) => (ERichCurveTangentWeightMode) _interpModes[keyIndex + 1];
    }
}