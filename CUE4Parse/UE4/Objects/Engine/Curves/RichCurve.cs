using System.Runtime.InteropServices;

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
    public readonly struct FRichCurveKey : IUStruct
    {
        public readonly ERichCurveInterpMode InterpMode;
        public readonly ERichCurveTangentMode TangentMode;
        public readonly ERichCurveTangentWeightMode TangentWeightMode;
        public readonly float Time;
        public readonly float Value;
        public readonly float ArriveTangent;
        public readonly float ArriveTangentWeight;
        public readonly float LeaveTangent;
        public readonly float LeaveTangentWeight;
    }
}