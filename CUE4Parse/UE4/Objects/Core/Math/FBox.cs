using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    public readonly struct FBox : IUStruct
    {
        /** Holds the box's minimum point. */
        public readonly FVector Min;
        /** Holds the box's maximum point. */
        public readonly FVector Max;
        /** Holds a flag indicating whether this box is valid. */
        public readonly bool bIsValid;

        public FBox(FAssetArchive Ar)
        {
            Min = Ar.Read<FVector>();
            Max = Ar.Read<FVector>();
            bIsValid = Ar.ReadFlag();
        }

        /// <summary>
        /// Creates and initializes a new box from the specified extents.
        /// </summary>
        /// <param name="min">The box's minimum point.</param>
        /// <param name="max">The box's maximum point.</param>
        public FBox(FVector min, FVector max)
        {
            Min = min;
            Max = max;
            bIsValid = true;
        }

        /// <summary>
        /// Creates and initializes a new box from an array of points.
        /// </summary>
        /// <param name="points">Array of Points to create for the bounding volume.</param>
        public FBox(FVector[] points)
        {
            Min = new FVector(0f, 0f, 0f);
            Max = new FVector(0f, 0f, 0f);
            bIsValid = true;
            foreach (FVector point in points)
            {
                //
            }
        }

        public FBox(FBox box)
        {
            Min = box.Min;
            Max = box.Max;
            bIsValid = box.bIsValid;
        }

        /* Fabian wtf is this hell :'( */

        public override string ToString() => $"IsValid={bIsValid}, Min=({Min}), Max=({Max})";
    }
}
