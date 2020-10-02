using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Engine.Curves
{
    public enum ERichCurveInterpMode : byte
	{
		/** Use linear interpolation between values. */
		RCIM_Linear,
		/** Use a constant value. Represents stepped values. */
		RCIM_Constant,
		/** Cubic interpolation. See TangentMode for different cubic interpolation options. */
		RCIM_Cubic,
		/** No interpolation. */
		RCIM_None
	};

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
	};

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
	};

	[StructLayout(LayoutKind.Sequential)]
    public readonly struct FRichCurveKey : IUStruct
    {
        public readonly ERichCurveInterpMode InterpMode;
		public readonly ERichCurveTangentMode TangentMode;
		public readonly ERichCurveTangentWeightMode TangentWeightMode;
		public readonly float KeyTime;
		public readonly float KeyValue;
		public readonly float ArriveTangent;
		public readonly float ArriveTangentWeight;
		public readonly float LeaveTangent;
		public readonly float LeaveTangentWeight;
	}
}
