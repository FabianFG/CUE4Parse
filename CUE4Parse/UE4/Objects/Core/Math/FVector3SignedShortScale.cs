namespace CUE4Parse.UE4.Objects.Core.Math
{
    public readonly struct FVector3SignedShortScale(short x, short y, short z, short w) : IUStruct
    {
        public readonly short X = x;
        public readonly short Y = y;
        public readonly short Z = z;
        public readonly short W = w;

        public static implicit operator FVector(FVector3SignedShortScale v)
        {
            // W having the value of short.MaxValue makes me believe I should use it (somehow) instead of a hardcoded constant
            float wf = v.W == 0 ? 1f : v.W;
            return new(v.X / wf, v.Y / wf, v.Z / wf);
        }
    }
}