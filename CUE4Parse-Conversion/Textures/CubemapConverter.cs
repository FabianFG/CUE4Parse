using System;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Textures
{
    public static class CubemapConverter
    {
        public static CTexture ToPanorama(this CTexture cubeMap)
        {
            float boundary = 0.005f; //TODO calculate value based on Texture Size?

            //preserve pixel density
            double widthSphereMap = Math.Sqrt(12) * cubeMap.Width;  // 2 * sqrt(3) * W
            double heightSphereMap = Math.Sqrt(3) * cubeMap.Width;  // sqrt(3) * W

            // Round width and height to integers
            int panoramaWidth = (int)Math.Floor(widthSphereMap);
            int panoramaHeight = (int)Math.Floor(heightSphereMap);

            byte[] panoramaData = new byte[panoramaWidth * panoramaHeight * 16]; // 16 bytes per pixel (4 floats)

            unsafe
            {
                fixed (byte* sphereDataPtr = panoramaData)
                fixed (byte* cubeDataPtr = cubeMap.Data)
                {
                    double sphereU, sphereV;
                    double phi, theta; // polar coordinates
                    double x, y, z; // unit vector

                    for (var j = 0; j < panoramaHeight; j++)
                    {
                        sphereV = 1 - ((double)j / panoramaHeight);
                        theta = sphereV * Math.PI;

                        for (var i = 0; i < panoramaWidth; i++)
                        {
                            sphereU = ((double)i / panoramaWidth);
                            phi = sphereU * 2 * Math.PI;

                            x = Math.Sin(phi) * Math.Sin(theta) * -1;
                            y = Math.Cos(theta);
                            z = Math.Cos(phi) * Math.Sin(theta) * -1;

                            MapCartesianToUv(x, y, z, out var u, out var v);

                            // Sample edges directly
                            if (u <= boundary || u >= 1 - boundary || IsCloseToVerticalEdge(v, boundary))
                            {
                                int xPixel = (int)(u * cubeMap.Width);
                                int yPixel = (int)(v * cubeMap.Height);
                                var color = GetColorFromCubeMap(cubeDataPtr, cubeMap, xPixel, yPixel);
                                SetPixel(sphereDataPtr, i, j, panoramaWidth, color);
                            }
                            else // Bilinear interpolation on non-edges
                            {
                                int x0 = Math.Max(0, Math.Min(cubeMap.Width - 1, (int)Math.Floor(u * cubeMap.Width)));
                                int x1 = Math.Max(0, Math.Min(cubeMap.Width - 1, x0 + 1));
                                int y0 = Math.Max(0, Math.Min(cubeMap.Height - 1, (int)Math.Floor(v * cubeMap.Height)));
                                int y1 = Math.Max(0, Math.Min(cubeMap.Height - 1, y0 + 1));

                                double weightX = u * cubeMap.Width - x0;
                                double weightY = v * cubeMap.Height - y0;

                                var color00 = GetColorFromCubeMap(cubeDataPtr, cubeMap, x0, y0);
                                var color01 = GetColorFromCubeMap(cubeDataPtr, cubeMap, x0, y1);
                                var color10 = GetColorFromCubeMap(cubeDataPtr, cubeMap, x1, y0);
                                var color11 = GetColorFromCubeMap(cubeDataPtr, cubeMap, x1, y1);

                                // Interpolate colors
                                var interpolatedColor = InterpolateColor(color00, color01, color10, color11, weightX, weightY);
                                SetPixel(sphereDataPtr, i, j, panoramaWidth, interpolatedColor);
                            }
                        }
                    }
                }
            }

            return new CTexture(panoramaWidth, panoramaHeight, EPixelFormat.PF_A32B32G32R32F, panoramaData);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void SetPixel(byte* dataPtr, int x, int y, int width, FLinearColor color)
        {
            int offset = (y * width + x) * 16; // 16 bytes per pixel (FLinearColor / 4 floats)

            *(FLinearColor*)(dataPtr + offset) = color;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe FLinearColor GetColorFromCubeMap(byte* cubeDataPtr, CTexture cubeMap, int x, int y)
        {
            int pixelOffset = (y * cubeMap.Width + x); // Base offset for the pixel (x, y)

            // Adjust pixelOffset based on the format
            switch (cubeMap.PixelFormat)
            {
                case EPixelFormat.PF_A32B32G32R32F:
                    pixelOffset *= 16; // 16 bytes per pixel (4x float32)
                    return new FLinearColor(
                        *(float*)(cubeDataPtr + pixelOffset + 12),    // R
                        *(float*)(cubeDataPtr + pixelOffset + 8),     // G
                        *(float*)(cubeDataPtr + pixelOffset + 4),     // B
                        *(float*)(cubeDataPtr + pixelOffset)          // A
                    ); // RGBA format

                case EPixelFormat.PF_R32G32B32F:
                    pixelOffset *= 12; // 12 bytes per pixel (3x float32)
                    return new FLinearColor(
                        1.0f,  // Alpha = 1.0
                        *(float*)(cubeDataPtr + pixelOffset + 8),  // B
                        *(float*)(cubeDataPtr + pixelOffset + 4),  // G
                        *(float*)(cubeDataPtr + pixelOffset)       // R
                    ); // RGBA format

                case EPixelFormat.PF_FloatRGBA:
                    pixelOffset *= 8; // 8 bytes per pixel (4x half16)
                    return new FLinearColor(
                        (float)*(Half*)(cubeDataPtr + pixelOffset + 6), // A
                        (float)*(Half*)(cubeDataPtr + pixelOffset + 4), // B
                        (float)*(Half*)(cubeDataPtr + pixelOffset + 2), // G
                        (float)*(Half*)(cubeDataPtr + pixelOffset)      // R
                    ); // RGBA format

                case EPixelFormat.PF_A16B16G16R16:
                    pixelOffset *= 8; // 8 bytes per pixel (4x half16)
                    return new FLinearColor(
                        (float)*(Half*)(cubeDataPtr + pixelOffset),     // A
                        (float)*(Half*)(cubeDataPtr + pixelOffset + 2), // B
                        (float)*(Half*)(cubeDataPtr + pixelOffset + 4), // G
                        (float)*(Half*)(cubeDataPtr + pixelOffset + 6)  // R
                    ); // RGBA format

                case EPixelFormat.PF_FloatRGB:
                    pixelOffset *= 6; // 6 bytes per pixel (3x half16)
                    return new FLinearColor(
                        1.0f, // Alpha = 1.0
                        (float)*(Half*)(cubeDataPtr + pixelOffset + 4), // B
                        (float)*(Half*)(cubeDataPtr + pixelOffset + 2), // G
                        (float)*(Half*)(cubeDataPtr + pixelOffset)      // R
                    ); // RGBA format

                case EPixelFormat.PF_R8G8B8A8:
                    // 4 bytes per pixel (R, G, B, A) 8 bits each
                    return new FLinearColor(
                        *(cubeDataPtr + pixelOffset + 2) / 255.0f,  // R
                        *(cubeDataPtr + pixelOffset + 1) / 255.0f,  // G
                        *(cubeDataPtr + pixelOffset) / 255.0f,      // B
                        *(cubeDataPtr + pixelOffset + 3) / 255.0f   // A
                    ); // RGBA format

                case EPixelFormat.PF_B8G8R8A8:
                    // 4 bytes per pixel (B, G, R, A) 8 bits each
                    return new FLinearColor(
                        *(cubeDataPtr + pixelOffset) / 255.0f,      // B
                        *(cubeDataPtr + pixelOffset + 1) / 255.0f,  // G
                        *(cubeDataPtr + pixelOffset + 2) / 255.0f,  // R
                        *(cubeDataPtr + pixelOffset + 3) / 255.0f   // A
                    ); // BGRA format

                case EPixelFormat.PF_R8:
                    // 1 byte per pixel (gray)
                    return new FLinearColor(
                        *(cubeDataPtr + pixelOffset) / 255.0f,  // R, G, B are all the same
                        *(cubeDataPtr + pixelOffset) / 255.0f,
                        *(cubeDataPtr + pixelOffset) / 255.0f,
                        1.0f  // Full alpha
                    ); // Grayscale format

                case EPixelFormat.PF_G16:
                    // 2 bytes per pixel (single channel 16-bit gray)
                    return new FLinearColor(
                        *(ushort*)(cubeDataPtr + pixelOffset) / 65535.0f, // Grayscale
                        *(ushort*)(cubeDataPtr + pixelOffset) / 65535.0f,
                        *(ushort*)(cubeDataPtr + pixelOffset) / 65535.0f,
                        1.0f  // Full alpha
                    ); // Grayscale format

                case EPixelFormat.PF_G16R16:
                    // 4 bytes per pixel (two channels, 16-bit each)
                    return new FLinearColor(
                        *(ushort*)(cubeDataPtr + pixelOffset) / 65535.0f,     // R channel
                        *(ushort*)(cubeDataPtr + pixelOffset + 2) / 65535.0f, // G channel
                        0.0f,  // B is not present
                        1.0f   // Full alpha
                    ); // Grayscale + channel

                default:
                    throw new NotImplementedException($"Unsupported pixel format: {cubeMap.PixelFormat}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static FLinearColor InterpolateColor(FLinearColor color00, FLinearColor color01, FLinearColor color10, FLinearColor color11, double weightX, double weightY)
        {
            var r = (float)((color00.R * (1 - weightX) * (1 - weightY)) + (color01.R * (1 - weightX) * weightY) +
                           (color10.R * weightX * (1 - weightY)) + (color11.R * weightX * weightY));

            var g = (float)((color00.G * (1 - weightX) * (1 - weightY)) + (color01.G * (1 - weightX) * weightY) +
                           (color10.G * weightX * (1 - weightY)) + (color11.G * weightX * weightY));

            var b = (float)((color00.B * (1 - weightX) * (1 - weightY)) + (color01.B * (1 - weightX) * weightY) +
                           (color10.B * weightX * (1 - weightY)) + (color11.B * weightX * weightY));

            var a = (float)((color00.A * (1 - weightX) * (1 - weightY)) + (color01.A * (1 - weightX) * weightY) +
                           (color10.A * weightX * (1 - weightY)) + (color11.A * weightX * weightY));

            return new FLinearColor(r, g, b, a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsCloseToVerticalEdge(double value, float boundary)
        {
            for (var i = 0; i <= 6; i++)
            {
                double verticalThresholdValue = i / 6.0;
                if (Math.Abs(value - verticalThresholdValue) <= boundary)
                {
                    return true;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MapCartesianToUv(double x, double y, double z, out double u, out double v)
        {
            double a = Math.Max(Math.Max(Math.Abs(x), Math.Abs(y)), Math.Abs(z));
            //parallel to unit vector
            double xa = x / a;
            double ya = y / a;
            double za = z / a;
            int faceIndex = 0;
            u = 0;
            v = 0;

            if (xa == 1)
            {
                //left (rotate 90)
                u = (ya + 1) / 2;
                v = (za + 1) / 2;
                faceIndex = 0;
            }
            else if (xa == -1)
            {
                //right (rotate 90)
                u = (1 - ya) / 2;
                v = (za + 1) / 2;
                faceIndex = 1;
            }
            else if (za == -1)
            {
                //back (flip y-axis / vertical)
                u = (xa + 1) / 2;
                v = (1 - ya) / 2;
                faceIndex = 2;
            }
            else if (za == 1)
            {
                //front
                u = (xa + 1) / 2;
                v = (ya + 1) / 2;
                faceIndex = 3;
            }
            else if (ya == 1)
            {
                //down (rotate 180 counter)
                u = (1 - xa) / 2;
                v = (za + 1) / 2;
                faceIndex = 5;
            }
            else if (ya == -1)
            {
                //up
                u = (xa + 1) / 2;
                v = (za + 1) / 2;
                faceIndex = 4;
            }
            v = (v + faceIndex) / 6;
        }
    }
}
