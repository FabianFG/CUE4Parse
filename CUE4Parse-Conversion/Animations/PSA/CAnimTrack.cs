using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.Utils;

namespace CUE4Parse_Conversion.Animations.PSA
{
    public class CAnimTrack
    {
        public FQuat[] KeyQuat = Array.Empty<FQuat>();
        public FVector[] KeyPos = Array.Empty<FVector>();
        public FVector[] KeyScale = Array.Empty<FVector>();
        public float[] KeyTime = Array.Empty<float>();

        public CAnimTrack() {}

        public CAnimTrack(int length)
        {
            KeyQuat = new FQuat[length];
            KeyPos = new FVector[length];
            KeyScale = new FVector[length];
        }

        // 3 time arrays; should be used either KeyTime or KeyQuatTime + KeyPosTime
        // When the corresponding array is empty, it will assume that Array[i] == i
        public float[] KeyQuatTime = Array.Empty<float>();
        public float[] KeyPosTime = Array.Empty<float>();
        public float[] KeyScaleTime = Array.Empty<float>();

        // DstPos and/or DstQuat will not be changed when KeyPos and/or KeyQuat are empty.
        public void GetBoneTransform(float frame, int frameCount, ref FQuat dstQuat, ref FVector dstPos, ref FVector dstScale)
        {
            // fast case: 1 frame only
            if (KeyTime.Length == 1 || frameCount == 1 || frame == 0.0f)
            {
                if (KeyQuat.Length > 0) dstQuat = KeyQuat[0];
                if (KeyPos.Length > 0) dstPos = KeyPos[0];
                if (KeyScale.Length > 0) dstScale = KeyScale[0];
                return;
            }

            // data for lerping
            int rotX, posX, scaX; // index of previous frame
            int rotY, posY, scaY; // index of next frame
            float rotF, posF, scaF; // fraction between X and Y for lerping

            var timeKeysCount = KeyTime.Length;
            var rotKeysCount = KeyQuat.Length;
            var posKeysCount = KeyPos.Length;
            var scaKeysCount = KeyScale.Length;

            if (timeKeysCount > 0)
            {
                // here: KeyPos and KeyQuat sizes either equals to 1 or equals to KeyTime size
                Trace.Assert(rotKeysCount == 1 || rotKeysCount == timeKeysCount);
                Trace.Assert(posKeysCount == 1 || posKeysCount == timeKeysCount);
                Trace.Assert(scaKeysCount == 1 || scaKeysCount == timeKeysCount);

                GetKeyParamsInternal(KeyTime, frame, out rotX, out rotY, out rotF);
                posX = scaX = rotX;
                posY = scaY = rotY;
                posF = scaF = rotF;

                if (rotKeysCount == 1) Reset(out rotX, out rotY, out rotF);
                if (posKeysCount == 1) Reset(out posX, out posY, out posF);
                if (scaKeysCount == 1) Reset(out scaX, out scaY, out scaF);
            }
            else
            {
                // empty KeyTime array - keys are evenly spaced on a time line
                // note: KeyPos and KeyQuat sizes can be different
                GetKeyParams(KeyQuatTime, frame, frameCount, rotKeysCount, out rotX, out rotY, out rotF);
                GetKeyParams(KeyPosTime, frame, frameCount, posKeysCount, out posX, out posY, out posF);
                GetKeyParams(KeyScaleTime, frame, frameCount, scaKeysCount, out scaX, out scaY, out scaF);
            }

            if (rotF > 0.0f) dstQuat = FQuat.Slerp(KeyQuat[rotX], KeyQuat[rotY], rotF);
            else if (rotKeysCount > 0) dstQuat = KeyQuat[rotX];

            if (posF > 0.0f) dstPos = MathUtils.Lerp(KeyPos[posX], KeyPos[posY], posF);
            else if (posKeysCount > 0) dstPos = KeyPos[posX];

            if (scaF > 0.0f) dstScale = MathUtils.Lerp(KeyScale[scaX], KeyScale[scaY], scaF);
            else if (scaKeysCount > 0) dstScale = KeyScale[scaX];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasKeys() => KeyQuat.Length + KeyPos.Length + KeyScale.Length > 0;

        // In:  KeyTime, Frame, NumFrames, Loop
        // Out: X - previous key index, Y - next key index, F - fraction between keys
        private static void GetKeyParams(float[] keyTime, float frame, int frameCount, int keyCount, out int x, out int y, out float f)
        {
            if (keyTime.Length > 0) GetKeyParamsInternal(keyTime, frame, out x, out y, out f);
            else if (keyCount > 1)
            {
                var position = frame / frameCount * keyCount;
                x = position.FloorToInt();
                f = position - x;
                y = x + 1;
                if (y >= keyCount)
                {
                    y = keyCount - 1;
                    f = 0.0f;
                }
            }
            else Reset(out x, out y, out f);
        }

        // In:  KeyTime, Frame, NumFrames, Loop
        // Out: X - previous key index, Y - next key index, F - fraction between keys
        private static void GetKeyParamsInternal(float[] keyTime, float frame, out int x, out int y, out float f)
        {
            x = FindTimeKey(keyTime, frame);

            y = x + 1;
            var numTimeKeys = keyTime.Length;
            if (y >= numTimeKeys)
            {
                y = numTimeKeys - 1;
                Trace.Assert(x == y);
                f = 0.0f;
            }
            else
            {
                f = (frame - keyTime[x]) / (keyTime[y] - keyTime[x]);
            }
        }

        /// <summary>
        /// find index in time key array
        /// </summary>
        /// <returns></returns>
        private static int FindTimeKey(float[] keyTime, float frame)
        {
            // *** binary search ***
            int low = 0, high = keyTime.Length - 1;
            while (low + Constants.MAX_ANIM_LINEAR_KEYS < high)
            {
                var mid = (low + high) / 2;
                if (frame < keyTime[mid])
                    high = mid - 1;
                else
                    low = mid;
            }

            // *** linear search ***
            int i;
            for (i = low; i <= high; i++)
            {
                var currKeyTime = keyTime[i];
                if (UnrealMath.IsNearlyEqual(frame, currKeyTime)) // exact key
                    return i;
                if (frame < currKeyTime) // previous key
                    return i > 0 ? i - 1 : 0;
            }

            if (i > high) i = high;
            return i;
        }

        private static void Reset(out int x, out int y, out float f)
        {
            x = y = 0;
            f = 0.0f;
        }
    }
}
