using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.Engine.Curves
{
    public readonly struct FSimpleCurve : IUStruct
    {
        public readonly ERichCurveInterpMode InterpMode;
        public readonly FSimpleCurveKey[] Keys;

        public FSimpleCurve(FAssetArchive Ar)
        {
            InterpMode = Ar.Read<ERichCurveInterpMode>();
            Keys = Ar.ReadArray<FSimpleCurveKey>();
        }
    }
}
