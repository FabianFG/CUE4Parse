using System;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public static class AnimationCompressionUtils
    {
        public const float Quant16BitDiv = 32767.0f;
        public const int Quant16BitOffs = 32767;
        public const float Quant10BitDiv = 511.0f;
        public const int Quant10BitOffs = 511;
        public const float Quant11BitDiv = 1023.0f;
        public const int Quant11BitOffs = 1023;

        /** normalized quaternion with 3 16-bit fixed point fields */
        public static FQuat ReadQuatFixed48NoW(this FArchive Ar, int componentMask = 7)
        {
            // 32767 corresponds to 0
            var x = (componentMask & 1) != 0 ? Ar.Read<ushort>() : Quant16BitOffs;
            var y = (componentMask & 2) != 0 ? Ar.Read<ushort>() : Quant16BitOffs;
            var z = (componentMask & 4) != 0 ? Ar.Read<ushort>() : Quant16BitOffs;

            var fX = (x - Quant16BitOffs) / Quant16BitDiv;
            var fY = (y - Quant16BitOffs) / Quant16BitDiv;
            var fZ = (z - Quant16BitOffs) / Quant16BitDiv;
            var wSquared = 1.0f - fX*fX - fY*fY - fZ*fZ;

            return new FQuat(fX, fY, fZ, wSquared > 0.0f ? MathF.Sqrt(wSquared) : 0.0f);
        }

        /** normalized quaternion with 11/11/10-bit fixed point fields */
        public static FQuat ReadQuatFixed32NoW(this FArchive Ar)
        {
            const int XShift = 21;
            const int YShift = 10;
            const uint ZMask = 0x000003ff;
            const uint YMask = 0x001ffc00;
            const uint XMask = 0xffe00000;

            var packed = Ar.Read<uint>();
            var unpackedX = packed >> XShift;
            var unpackedY = (packed & YMask) >> YShift;
            var unpackedZ = (packed & ZMask);

            var x = ((int) unpackedX - Quant11BitOffs) / Quant11BitDiv;
            var y = ((int) unpackedY - Quant11BitOffs) / Quant11BitDiv;
            var z = ((int) unpackedZ - Quant10BitOffs) / Quant10BitDiv;
            var wSquared = 1.0f - x*x - y*y - z*z;

            return new FQuat(x, y, z, wSquared > 0.0f ? MathF.Sqrt(wSquared) : 0.0f);
        }

        public static FQuat ReadQuatFloat96NoW(this FArchive Ar)
        {
            var x = Ar.Read<float>();
            var y = Ar.Read<float>();
            var z = Ar.Read<float>();
            var wSquared = 1.0f - x*x - y*y - z*z;

            return new FQuat(x, y, z, wSquared > 0.0f ? MathF.Sqrt(wSquared) : 0.0f);
        }

        public static FVector ReadVectorFixed48(this FArchive Ar)
        {
            var x = Ar.Read<ushort>();
            var y = Ar.Read<ushort>();
            var z = Ar.Read<ushort>();

            var fX = (x - Quant16BitOffs) / Quant16BitDiv;
            var fY = (y - Quant16BitOffs) / Quant16BitDiv;
            var fZ = (z - Quant16BitOffs) / Quant16BitDiv;

            return new FVector(fX * 128.0f, fY * 128.0f, fZ * 128.0f);
        }

        public static FVector ReadVectorIntervalFixed32(this FArchive Ar, FVector mins, FVector ranges)
        {
            const int ZShift = 21;
            const int YShift = 10;
            const uint XMask = 0x000003ff;
            const uint YMask = 0x001ffc00;
            const uint ZMask = 0xffe00000;

            var packed = Ar.Read<uint>();
            var unpackedZ = packed >> ZShift;
            var unpackedY = (packed & YMask) >> YShift;
            var unpackedX = (packed & XMask);

            var x = (((int) unpackedX - Quant10BitOffs) / Quant10BitDiv) * ranges.X + mins.X;
            var y = (((int) unpackedY - Quant11BitOffs) / Quant11BitDiv) * ranges.Y + mins.Y;
            var z = (((int) unpackedZ - Quant11BitOffs) / Quant11BitDiv) * ranges.Z + mins.Z;

            return new FVector(x, y, z);
        }

        public static FQuat ReadQuatIntervalFixed32NoW(this FArchive Ar, FVector mins, FVector ranges)
        {
            const int XShift = 21;
            const int YShift = 10;
            const uint ZMask = 0x000003ff;
            const uint YMask = 0x001ffc00;
            const uint XMask = 0xffe00000;

            var packed = Ar.Read<uint>();
            var unpackedX = packed >> XShift;
            var unpackedY = (packed & YMask) >> YShift;
            var unpackedZ = (packed & ZMask);

            var x = (((int) unpackedX - Quant11BitOffs) / Quant11BitDiv) * ranges.X + mins.X;
            var y = (((int) unpackedY - Quant11BitOffs) / Quant11BitDiv) * ranges.Y + mins.Y;
            var z = (((int) unpackedZ - Quant10BitOffs) / Quant10BitDiv) * ranges.Z + mins.Z;
            var wSquared = 1.0f - x*x - y*y - z*z;

            return new FQuat(x, y, z, wSquared > 0.0f ? MathF.Sqrt(wSquared) : 0.0f);
        }

        public static FQuat ReadQuatFloat32NoW(this FArchive Ar)
        {
            const int XShift = 21;
            const int YShift = 10;
            const uint ZMask = 0x000003ff;
            const uint YMask = 0x001ffc00;
            const uint XMask = 0xffe00000;

            var packed = Ar.Read<uint>();
            var unpackedX = packed >> XShift;
            var unpackedY = (packed & YMask) >> YShift;
            var unpackedZ = (packed & ZMask);

            var x = BitConverter.Int32BitsToSingle((int) (((((unpackedX >> 7) & 7) + 123) << 23) | ((unpackedX & 0x7F | 32 * (unpackedX & 0xFFFFFC00)) << 16)));
            var y = BitConverter.Int32BitsToSingle((int) (((((unpackedY >> 7) & 7) + 123) << 23) | ((unpackedY & 0x7F | 32 * (unpackedY & 0xFFFFFC00)) << 16)));
            var z = BitConverter.Int32BitsToSingle((int) (((((unpackedZ >> 6) & 7) + 123) << 23) | ((unpackedZ & 0x3F | 32 * (unpackedZ & 0xFFFFFE00)) << 17)));
            var wSquared = 1.0f - x*x - y*y - z*z;

            return new FQuat(x, y, z, wSquared > 0.0f ? MathF.Sqrt(wSquared) : 0.0f);
        }

        public static float DecodeFixed48_PerTrackComponent(ushort value, int log2)
        {
            var offset = (1 << (15 - log2)) - 1; // default (for log2==7) is 255
            var invFactor = 1.0f / (offset >> log2); // default is 1.0f
            return (value - offset) * invFactor;
        }
    }
}