using System;
using System.Runtime.CompilerServices;

namespace CUE4Parse_Conversion.Textures.BC
{
    public static class BCDecoder
    {
        public static byte[] BC4(byte[] inp, int sizeX, int sizeY, int sizeZ)
        {
            byte[] ret = new byte[sizeX * sizeY * sizeZ * 4];
            unsafe
            {
                fixed (byte* bytePtr = inp)
                {
                    int index = 0;
                    byte* temp = bytePtr;
                    for (var z = 0; z < sizeZ; z++)
                    {
                        for (int y = 0; y < sizeY / 4; y++)
                        {
                            for (int x = 0; x < sizeX / 4; x++)
                            {
                                var r_bytes = DecodeBCBlock(temp, ref index);
                                for (int i = 0; i < 16; i++)
                                {
                                    ret[GetPixelLoc(sizeX, sizeY, x * 4 + (i % 4), y * 4 + (i / 4), z, 4, 0)] = r_bytes[i];
                                }
                            }
                        }
                    }
                }
            }
            return ret;
        }

        public static byte[] BC5(byte[] inp, int sizeX, int sizeY, int sizeZ)
        {
            byte[] ret = new byte[sizeX * sizeY * sizeZ * 4];
            unsafe
            {
                fixed (byte* bytePtr = inp)
                {
                    int index = 0;
                    byte* temp = bytePtr;
                    for (var z = 0; z < sizeZ; z++)
                    {
                        for (int y = 0; y < sizeY / 4; y++)
                        {
                            for (int x = 0; x < sizeX / 4; x++)
                            {
                                var r_bytes = DecodeBCBlock(temp, ref index);
                                var g_bytes = DecodeBCBlock(temp, ref index);
                                for (int i = 0; i < 16; i++)
                                {
                                    ret[GetPixelLoc(sizeX, sizeY, x * 4 + (i % 4), y * 4 + (i / 4), z, 4, 0)] = r_bytes[i];
                                    ret[GetPixelLoc(sizeX, sizeY, x * 4 + (i % 4), y * 4 + (i / 4), z, 4, 1)] = g_bytes[i];
                                    ret[GetPixelLoc(sizeX, sizeY, x * 4 + (i % 4), y * 4 + (i / 4), z, 4, 2)] = GetZNormal(r_bytes[i], g_bytes[i]);
                                }
                            }
                        }
                    }
                }
            }
            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetPixelLoc(int width, int height, int x, int y, int z, int bpp, int off) => (z * width * height * bpp) + (y * width * bpp) + (x * bpp) + off;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte GetZNormal(byte x, byte y)
        {
            var xf = (x / 127.5f) - 1;
            var yf = (y / 127.5f) - 1;
            var zval = 1 - xf * xf - yf * yf;
            var zval_ = (float)Math.Sqrt(zval > 0 ? zval : 0);
            zval = zval_ < 1 ? zval_ : 1;
            return (byte)((zval * 127) + 128);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void Read(byte* destPtr, byte* srcPtr, ref int index, int length)
        {
            Unsafe.CopyBlockUnaligned(ref destPtr[0], ref srcPtr[index], (uint)length);
            index += length;
        }

        private static unsafe byte[] DecodeBCBlock(byte* data, ref int index)
        {
            byte* arr = stackalloc byte[3];
            float ref0 = data[index++];
            float ref1 = data[index++];

            float[] ref_sl = new float[8];
            ref_sl[0] = ref0;
            ref_sl[1] = ref1;

            if (ref0 > ref1)
            {
                ref_sl[2] = (6 * ref0 + 1 * ref1) / 7;
                ref_sl[3] = (5 * ref0 + 2 * ref1) / 7;
                ref_sl[4] = (4 * ref0 + 3 * ref1) / 7;
                ref_sl[5] = (3 * ref0 + 4 * ref1) / 7;
                ref_sl[6] = (2 * ref0 + 5 * ref1) / 7;
                ref_sl[7] = (1 * ref0 + 6 * ref1) / 7;
            }
            else
            {
                ref_sl[2] = (4 * ref0 + 1 * ref1) / 5;
                ref_sl[3] = (3 * ref0 + 2 * ref1) / 5;
                ref_sl[4] = (2 * ref0 + 3 * ref1) / 5;
                ref_sl[5] = (1 * ref0 + 4 * ref1) / 5;
                ref_sl[6] = 0;
                ref_sl[7] = 255;
            }

            Read(arr, data, ref index, 3);
            byte[] index_block1 = GetBCIndices(arr);

            Read(arr, data, ref index, 3);
            byte[] index_block2 = GetBCIndices(arr);

            byte[] bytes = new byte[16];
            for (int i = 0; i < 8; i++)
            {
                bytes[7 - i] = (byte)ref_sl[index_block1[i]];
            }
            for (int i = 0; i < 8; i++)
            {
                bytes[15 - i] = (byte)ref_sl[index_block2[i]];
            }

            return bytes;
        }

        private static unsafe byte[] GetBCIndices(byte* data) =>
            new byte[] {
                (byte)((data[2] & 0b1110_0000) >> 5),
                (byte)((data[2] & 0b0001_1100) >> 2),
                (byte)(((data[2] & 0b0000_0011) << 1) | ((data[1] & 0b1 << 7) >> 7)),
                (byte)((data[1] & 0b0111_0000) >> 4),
                (byte)((data[1] & 0b0000_1110) >> 1),
                (byte)(((data[1] & 0b0000_0001) << 2) | ((data[0] & 0b11 << 6) >> 6)),
                (byte)((data[0] & 0b0011_1000) >> 3),
                (byte)(data[0] & 0b0000_0111)
            };
    }
}
