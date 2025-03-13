namespace CUE4Parse_Conversion.Textures.DXT;

/// <summary>
/// https://gist.github.com/soeminnminn/e9c4c99867743a717f5b
/// </summary>
public static class DXTDecoder
{
    public static byte[] DXT1(byte[] inp, int sizeX, int sizeY, int sizeZ)
    {
        int bitsPerSecond = sizeX * Constants.DXT_BITS_PER_PIXEL;
        int sizeOfPlane = bitsPerSecond * sizeY;
        byte[] rawData = new byte[sizeZ * sizeOfPlane];
        Colour8888[] colours = new Colour8888[4];
        colours[0].Alpha = 0xFF;
        colours[1].Alpha = 0xFF;
        colours[2].Alpha = 0xFF;

        unsafe
        {
            fixed (byte* bytePtr = inp)
            {
                byte* temp = bytePtr;
                for (int z = 0; z < sizeZ; z++)
                {
                    for (int y = 0; y < sizeY; y += 4)
                    {
                        for (int x = 0; x < sizeX; x += 4)
                        {
                            ushort colour0 = *((ushort*)temp);
                            ushort colour1 = *((ushort*)(temp + 2));
                            DxtcReadColor(colour0, ref colours[0]);
                            DxtcReadColor(colour1, ref colours[1]);

                            uint bitmask = ((uint*)temp)[1];
                            temp += 8;

                            if (colour0 > colour1)
                            {
                                // Four-color block: derive the other two colors.
                                // 00 = color_0, 01 = color_1, 10 = color_2, 11 = color_3
                                // These 2-bit codes correspond to the 2-bit fields
                                // stored in the 64-bit block.
                                colours[2].Blue = (byte)((2 * colours[0].Blue + colours[1].Blue) / 3);
                                colours[2].Green = (byte)((2 * colours[0].Green + colours[1].Green) / 3);
                                colours[2].Red = (byte)((2 * colours[0].Red + colours[1].Red) / 3);
                                // colours[2].Alpha = 0xFF;

                                colours[3].Blue = (byte)((colours[0].Blue + 2 * colours[1].Blue) / 3);
                                colours[3].Green = (byte)((colours[0].Green + 2 * colours[1].Green) / 3);
                                colours[3].Red = (byte)((colours[0].Red + 2 * colours[1].Red) / 3);
                                colours[3].Alpha = 0xFF;
                            }
                            else
                            {
                                // Three-color block: derive the other color.
                                // 00 = color_0,  01 = color_1,  10 = color_2,
                                // 11 = transparent.
                                // These 2-bit codes correspond to the 2-bit fields
                                // stored in the 64-bit block.
                                colours[2].Blue = (byte)((colours[0].Blue + colours[1].Blue) / 2);
                                colours[2].Green = (byte)((colours[0].Green + colours[1].Green) / 2);
                                colours[2].Red = (byte)((colours[0].Red + colours[1].Red) / 2);
                                // colours[2].Alpha = 0xFF;

                                colours[3].Blue = 0x00;
                                colours[3].Green = 0x00;
                                colours[3].Red = 0x00;
                                colours[3].Alpha = 0x00;
                            }

                            for (int j = 0, k = 0; j < 4; j++)
                            {
                                for (int i = 0; i < 4; i++, k++)
                                {
                                    int select = (int)((bitmask & (0x03 << k * 2)) >> k * 2);
                                    Colour8888 col = colours[select];
                                    if (((x + i) < sizeX) && ((y + j) < sizeY))
                                    {
                                        uint offset = (uint)(z * sizeOfPlane + (y + j) * bitsPerSecond + (x + i) * Constants.DXT_BITS_PER_PIXEL);
                                        rawData[offset + 0] = col.Red;
                                        rawData[offset + 1] = col.Green;
                                        rawData[offset + 2] = col.Blue;
                                        rawData[offset + 3] = col.Alpha;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return rawData;
    }

    public static byte[] DXT5(byte[] inp, int sizeX, int sizeY, int sizeZ)
    {
        int bitsPerSecond = sizeX * Constants.DXT_BITS_PER_PIXEL;
        int sizeOfPlane = bitsPerSecond * sizeY;
        byte[] rawData = new byte[sizeZ * sizeOfPlane];
        Colour8888[] colours = new Colour8888[4];
        ushort[] alphas = new ushort[8];

        unsafe
        {
            fixed (byte* bytePtr = inp)
            {
                byte* temp = bytePtr;
                for (int z = 0; z < sizeZ; z++)
                {
                    for (int y = 0; y < sizeY; y += 4)
                    {
                        for (int x = 0; x < sizeX; x += 4)
                        {
                            if (y >= sizeY || x >= sizeX)
                                break;

                            alphas[0] = temp[0];
                            alphas[1] = temp[1];
                            byte* alphamask = (temp + 2);
                            temp += 8;

                            DxtcReadColors(temp, colours);
                            uint bitmask = ((uint*)temp)[1];
                            temp += 8;

                            // Four-color block: derive the other two colors.
                            // 00 = color_0, 01 = color_1, 10 = color_2, 11	= color_3
                            // These 2-bit codes correspond to the 2-bit fields
                            // stored in the 64-bit block.
                            colours[2].Blue = (byte)((2 * colours[0].Blue + colours[1].Blue + 1) / 3);
                            colours[2].Green = (byte)((2 * colours[0].Green + colours[1].Green + 1) / 3);
                            colours[2].Red = (byte)((2 * colours[0].Red + colours[1].Red + 1) / 3);
                            //colours[2].alpha = 0xFF;

                            colours[3].Blue = (byte)((colours[0].Blue + 2 * colours[1].Blue + 1) / 3);
                            colours[3].Green = (byte)((colours[0].Green + 2 * colours[1].Green + 1) / 3);
                            colours[3].Red = (byte)((colours[0].Red + 2 * colours[1].Red + 1) / 3);
                            //colours[3].alpha = 0xFF;

                            int k = 0;
                            for (int j = 0; j < 4; j++)
                            {
                                for (int i = 0; i < 4; k++, i++)
                                {
                                    int select = (int)((bitmask & (0x03 << k * 2)) >> k * 2);
                                    Colour8888 col = colours[select];
                                    // only put pixels out < width or height
                                    if (((x + i) < sizeX) && ((y + j) < sizeY))
                                    {
                                        uint offset = (uint)(z * sizeOfPlane + (y + j) * bitsPerSecond + (x + i) * Constants.DXT_BITS_PER_PIXEL);
                                        rawData[offset] = col.Red;
                                        rawData[offset + 1] = col.Green;
                                        rawData[offset + 2] = col.Blue;
                                    }
                                }
                            }

                            // 8-alpha or 6-alpha block?
                            if (alphas[0] > alphas[1])
                            {
                                // 8-alpha block:  derive the other six alphas.
                                // Bit code 000 = alpha_0, 001 = alpha_1, others are interpolated.
                                alphas[2] = (ushort)((6 * alphas[0] + 1 * alphas[1] + 3) / 7); // bit code 010
                                alphas[3] = (ushort)((5 * alphas[0] + 2 * alphas[1] + 3) / 7); // bit code 011
                                alphas[4] = (ushort)((4 * alphas[0] + 3 * alphas[1] + 3) / 7); // bit code 100
                                alphas[5] = (ushort)((3 * alphas[0] + 4 * alphas[1] + 3) / 7); // bit code 101
                                alphas[6] = (ushort)((2 * alphas[0] + 5 * alphas[1] + 3) / 7); // bit code 110
                                alphas[7] = (ushort)((1 * alphas[0] + 6 * alphas[1] + 3) / 7); // bit code 111
                            }
                            else
                            {
                                // 6-alpha block.
                                // Bit code 000 = alpha_0, 001 = alpha_1, others are interpolated.
                                alphas[2] = (ushort)((4 * alphas[0] + 1 * alphas[1] + 2) / 5); // Bit code 010
                                alphas[3] = (ushort)((3 * alphas[0] + 2 * alphas[1] + 2) / 5); // Bit code 011
                                alphas[4] = (ushort)((2 * alphas[0] + 3 * alphas[1] + 2) / 5); // Bit code 100
                                alphas[5] = (ushort)((1 * alphas[0] + 4 * alphas[1] + 2) / 5); // Bit code 101
                                alphas[6] = 0x00; // Bit code 110
                                alphas[7] = 0xFF; // Bit code 111
                            }

                            // Note: Have to separate the next two loops,
                            // it operates on a 6-byte system.

                            // First three bytes
                            //uint bits = (uint)(alphamask[0]);
                            uint bits = (uint)((alphamask[0]) | (alphamask[1] << 8) | (alphamask[2] << 16));
                            for (int j = 0; j < 2; j++)
                            {
                                for (int i = 0; i < 4; i++)
                                {
                                    // only put pixels out < width or height
                                    if (((x + i) < sizeX) && ((y + j) < sizeY))
                                    {
                                        uint offset = (uint)(z * sizeOfPlane + (y + j) * bitsPerSecond + (x + i) * Constants.DXT_BITS_PER_PIXEL + 3);
                                        rawData[offset] = (byte)alphas[bits & 0x07];
                                    }
                                    bits >>= 3;
                                }
                            }

                            // Last three bytes
                            //bits = (uint)(alphamask[3]);
                            bits = (uint)((alphamask[3]) | (alphamask[4] << 8) | (alphamask[5] << 16));
                            for (int j = 2; j < 4; j++)
                            {
                                for (int i = 0; i < 4; i++)
                                {
                                    // only put pixels out < width or height
                                    if (((x + i) < sizeX) && ((y + j) < sizeY))
                                    {
                                        uint offset = (uint)(z * sizeOfPlane + (y + j) * bitsPerSecond + (x + i) * Constants.DXT_BITS_PER_PIXEL + 3);
                                        rawData[offset] = (byte)alphas[bits & 0x07];
                                    }
                                    bits >>= 3;
                                }
                            }
                        }
                    }
                }
            }
            return rawData;
        }
    }

    private static unsafe void DxtcReadColors(byte* data, Colour8888[] op)
    {
        byte buf = (byte)((data[1] & 0xF8) >> 3);
        op[0].Red = (byte)(buf << 3 | buf >> 2);
        buf = (byte)(((data[0] & 0xE0) >> 5) | ((data[1] & 0x7) << 3));
        op[0].Green = (byte)(buf << 2 | buf >> 4);
        buf = (byte)(data[0] & 0x1F);
        op[0].Blue = (byte)(buf << 3 | buf >> 2);

        buf = (byte)((data[3] & 0xF8) >> 3);
        op[1].Red = (byte)(buf << 3 | buf >> 2);
        buf = (byte)(((data[2] & 0xE0) >> 5) | ((data[3] & 0x7) << 3));
        op[1].Green = (byte)(buf << 2 | buf >> 4);
        buf = (byte)(data[2] & 0x1F);
        op[1].Blue = (byte)(buf << 3 | buf >> 2);
    }

    private static void DxtcReadColor(ushort data, ref Colour8888 op)
    {
        byte buf = (byte)((data & 0xF800) >> 11);
        op.Red = (byte)(buf << 3 | buf >> 2);
        buf = (byte)((data & 0x7E0) >> 5);
        op.Green = (byte)(buf << 2 | buf >> 4);
        buf = (byte)(data & 0x1f);
        op.Blue = (byte)(buf << 3 | buf >> 2);
    }
}
