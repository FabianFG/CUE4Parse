using System;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace CUE4Parse_Conversion.Textures.BC
{
    public static class Detex
    {
        static Detex()
        {
            PrepareDllFile();
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct detexTexture
        {
            public uint format;
            public byte* data;
            public int width;
            public int height;
            public int width_in_blocks;
            public int height_in_blocks;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] DecodeDetexLinear(byte[] inp, int width, int height, bool isFloat, DetexTextureFormat inputFormat, DetexPixelFormat outputPixelFormat)
        {
            var dst = new byte[width * height * (isFloat ? 16 : 4)];
            DecodeDetexLinear(inp, dst, width, height, inputFormat, outputPixelFormat);
            return dst;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool DecodeDetexLinear(byte[] inp, byte[] dst, int width, int height, DetexTextureFormat inputFormat, DetexPixelFormat outputPixelFormat)
        {
            unsafe
            {
                fixed (byte* inpPtr = inp, dstPtr = dst)
                {
                    detexTexture tex;
                    tex.format = (uint)inputFormat;
                    tex.data = inpPtr;
                    tex.width = width;
                    tex.height = height;
                    tex.width_in_blocks = width / 4;
                    tex.height_in_blocks = height / 4;
                    return detexDecompressTextureLinear(&tex, dstPtr,
                        (uint)outputPixelFormat);
                }
            }
        }

        [DllImport(Constants.DETEX_DLL_NAME)]
        private static extern unsafe bool detexDecompressTextureLinear(detexTexture* texture, byte* pixelBuffer,
            uint pixelFormat);
        
        private static void PrepareDllFile()
        {
            if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
                return;
            
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CUE4Parse_Conversion.Resources.Detex.dll");
            if (stream == null)
                throw new MissingManifestResourceException("Couldn't find Detex.dll in Embedded Resources");
            
            var ba = new byte[(int)stream.Length];
            stream.ReadExactly(ba);

            var dllFile = Constants.DETEX_DLL_NAME + ".dll";
            
            if (File.Exists(dllFile))
            {
                var bb = File.ReadAllBytes(dllFile);
                var fileHash = SHA1.HashData(ba).AsSpan();
                var fileHash2 = SHA1.HashData(bb).AsSpan();

                if (fileHash.SequenceEqual(fileHash2))
                    return;
            }

            File.WriteAllBytes(dllFile, ba);
        }
    }
}
