using SkiaSharp;
using System;
namespace CUE4Parse_Conversion.Textures
{
    public static class CubemapConverter
    {
        public static SKBitmap ToPanorama(this SKBitmap cubeMap)
        {
            float boundary = 0.002f;
            double widthSphereMap = cubeMap.Width * Math.Sqrt(4.0 * Math.PI);
            double heightSphereMap = widthSphereMap / 2;

            var sphereMap = new SKBitmap((int)widthSphereMap, (int)heightSphereMap);

            double sphereU, sphereV;
            double phi, theta; //polar coordinates
            double x, y, z; //unit vector

            for (var j = 0; j < sphereMap.Height; j++)
            {
                sphereV = 1 - ((double)j / sphereMap.Height);
                theta = sphereV * Math.PI;

                for (var i = 0; i < sphereMap.Width; i++)
                {
                    sphereU = ((double)i / sphereMap.Width);
                    phi = sphereU * 2 * Math.PI;

                    x = Math.Sin(phi) * Math.Sin(theta) * -1;
                    y = Math.Cos(theta);
                    z = Math.Cos(phi) * Math.Sin(theta) * -1; 

                    MapCartesianToUv(x, y, z, out var u, out var v);

                    //sample edges directly
                    if (u <= boundary || u >= 1- boundary || IsCloseToVerticalEdge(v, boundary))
                    {
                        int xPixel = (int)(u * cubeMap.Width);
                        int yPixel = (int)(v * cubeMap.Height);
                        sphereMap.SetPixel(i, j, cubeMap.GetPixel(xPixel, yPixel));
                    }
                    else //bilinear interpolation on non-edges
                    {
                        int x0 = Math.Max(0, Math.Min(cubeMap.Width - 1, (int)Math.Floor(u * cubeMap.Width)));
                        int x1 = Math.Max(0, Math.Min(cubeMap.Width - 1, x0 + 1));
                        int y0 = Math.Max(0, Math.Min(cubeMap.Height - 1, (int)Math.Floor(v * cubeMap.Height)));
                        int y1 = Math.Max(0, Math.Min(cubeMap.Height - 1, y0 + 1));

                        double weightX = u * cubeMap.Width - x0;
                        double weightY = v * cubeMap.Height - y0;

                        var color00 = cubeMap.GetPixel(x0, y0);
                        var color01 = cubeMap.GetPixel(x0, y1);
                        var color10 = cubeMap.GetPixel(x1, y0);
                        var color11 = cubeMap.GetPixel(x1, y1);

                        byte r = (byte)(color00.Red * (1 - weightX) * (1 - weightY) + color01.Red * (1 - weightX) * weightY +
                                        color10.Red * weightX * (1 - weightY) + color11.Red * weightX * weightY);
                        byte g = (byte)(color00.Green * (1 - weightX) * (1 - weightY) + color01.Green * (1 - weightX) * weightY +
                                        color10.Green * weightX * (1 - weightY) + color11.Green * weightX * weightY);
                        byte b = (byte)(color00.Blue * (1 - weightX) * (1 - weightY) + color01.Blue * (1 - weightX) * weightY +
                                        color10.Blue * weightX * (1 - weightY) + color11.Blue * weightX * weightY);
                        sphereMap.SetPixel(i, j, new SKColor(r, g, b));
                    }
                }
            }
            return sphereMap;
        }
        static bool IsCloseToVerticalEdge(double value, float boundary)
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
        private static void MapCartesianToUv(double x, double y, double z,out double u, out double v)
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