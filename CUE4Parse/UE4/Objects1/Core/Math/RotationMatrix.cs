namespace CUE4Parse.UE4.Objects.Core.Math
{
    /** Rotation matrix no translation */
    public sealed class FRotationMatrix : FRotationTranslationMatrix
    {
        public FRotationMatrix(FRotator rot) : base(rot, new FVector()) { }
    }
}