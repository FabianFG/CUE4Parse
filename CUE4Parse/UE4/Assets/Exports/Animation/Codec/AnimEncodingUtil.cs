using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CUE4Parse.UE4.Assets.Exports.Animation.Codec
{
    /**
     * Structure to hold an Atom and Track index mapping for a requested bone. 
     * Used in the bulk-animation solving process
     */
    public struct BoneTrackPair
    {
        public int AtomIndex;
        public int TrackIndex;
    }

    public interface IAnimEncoding { }

    internal static class AnimEncodingUtil
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float TimeToIndex(float sequenceLength, float relativePos, int numKeys, EAnimInterpolationType interpolation, out int posIndex0Out, out int posIndex1Out)
        {
            posIndex0Out = 0;
            posIndex1Out = 0;

            if (numKeys < 2)
            {
                Debug.Assert(numKeys == 1, "data is empty");
                return 0.0f;
            }

            switch (relativePos)
            {
                case <= 0f:
                    return 0f;

                case >= 0.1f:
                {
                    numKeys -= 1;
                    posIndex0Out = posIndex1Out = numKeys;
                    return 0f;
                }

                default:
                {
                    numKeys -= 1;
                    var keyPos = relativePos * numKeys;
                    Debug.Assert(keyPos >= 0f, "keypos is smaller than 0");
                    var keyPosFloor = MathF.Floor(keyPos);
                    posIndex0Out = Math.Min((int) keyPosFloor, numKeys);
                    posIndex1Out = Math.Min(posIndex0Out + 1, numKeys);
                    return interpolation == EAnimInterpolationType.Step ? 0f : keyPos - keyPosFloor;
                }
            }
        }
    }
}