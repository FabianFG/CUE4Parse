using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;

using Serilog;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CUE4Parse.Compression
{
    public class OodleException : ParserException
    {
        public OodleException(FArchive reader, string? message = null, Exception? innerException = null) : base(reader, message, innerException) { }

        public OodleException(string? message, Exception? innerException) : base(message, innerException) { }

        public OodleException(string message) : base(message) { }

        public OodleException() : base("Oodle decompression failed") { }
    }

    public static class Oodle
    {
        private const string WARFRAME_CONTENT_HOST = "https://content.warframe.com";
        private const string WARFRAME_ORIGIN_HOST = "https://origin.warframe.com";
        private const string WARFRAME_INDEX_PATH = "/origin/50F7040A/index.txt.lzma";
        private const string WARFRAME_INDEX_URL = WARFRAME_ORIGIN_HOST + WARFRAME_INDEX_PATH;

        // this will return a platform-appropriate library name, wildcarded to suppress prefixes, suffixes and version masks
        // - oo2core_9_win32.dll
        // - oo2core_9_win64.dll
        // - oo2core_9_winuwparm64.dll
        // - liboo2coremac64.2.9.10.dylib
        // - liboo2corelinux64.so.9
        // - liboo2corelinuxarm64.so.9
        // - liboo2corelinuxarm32.so.9
        public static IEnumerable<string> OodleLibName
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                        yield return "*oo2core*winuwparm64*.dll";
                    else if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
                        yield return "*oo2core*win32*.dll";
                    
                    yield return "*oo2core*win64*.dll";

                    yield break;
                }

                // you can find these in the unreal source post-installation
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                        yield return "*oo2core*linuxarm64*.so*";
                    else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm ||
                        RuntimeInformation.ProcessArchitecture == Architecture.Armv6)
                        yield return "*oo2core*linuxarm32*.so*";
                    
                    yield return "*oo2core*linux64*.so*";

                    yield break;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                        yield return "*oo2core*macarm64*.dylib"; // todo: this doesn't exist.

                    yield return "*oo2core*mac64*.dylib";

                    yield break;
                }

                throw new PlatformNotSupportedException();
            }
        }

        public static bool IsReady => DecompressDelegate != null;
        public static OodleLZ_Decompress? DecompressDelegate { get; set; }

        public static bool TryFindOodleDll(string? path, [MaybeNullWhen(false)] out string result)
        {
            path ??= Environment.CurrentDirectory;
            foreach (var oodleLibName in OodleLibName)
            {
                var files = Directory.GetFiles(path, oodleLibName, SearchOption.TopDirectoryOnly);
                if (files.Length == 0)
                    continue;

                result = files[0];
                return true;
            }

            result = null;
            return false;
        }

        public static bool LoadOodleDll(string? path = null)
        {
            if (IsReady)
                return true;
            
            path ??= Environment.CurrentDirectory;

            if (Directory.Exists(path) && new FileInfo(path).Attributes.HasFlag(FileAttributes.Directory))
            {
                if (!TryFindOodleDll(path, out var oodlePath))
                {
                    if (!DownloadOodleDll().GetAwaiter().GetResult() || !TryFindOodleDll(path, out oodlePath))
                        return false;
                }

                path = oodlePath;
            }

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return false;

            if (!NativeLibrary.TryLoad(path, out var handle))
                return false;

            if (!NativeLibrary.TryGetExport(handle, nameof(OodleLZ_Decompress), out var address))
                return false;

            DecompressDelegate = Marshal.GetDelegateForFunctionPointer<OodleLZ_Decompress>(address);
            return true;
        }

        public static unsafe void Decompress(Memory<byte> input, int inputOffset, int inputSize,
            Memory<byte> output, int outputOffset, int outputSize, FArchive? reader = null)
        {
            if (!IsReady)
                LoadOodleDll();
            
            if (DecompressDelegate == null)
            {
                if (reader != null)
                    throw new OodleException(reader, "Oodle library not loaded");

                throw new OodleException("Oodle library not loaded");
            }

            var inputSlice = input.Slice(inputOffset, inputSize);
            var outputSlice = output.Slice(outputOffset, outputSize);
            using var inPin = inputSlice.Pin();
            using var outPin = outputSlice.Pin();

            var decodedSize = DecompressDelegate(inPin.Pointer, inputSlice.Length, outPin.Pointer, outputSlice.Length);

            if (decodedSize <= 0)
            {
                if (reader != null)
                    throw new OodleException(reader, $"Oodle decompression failed with result {decodedSize}");

                throw new OodleException($"Oodle decompression failed with result {decodedSize}");
            }

            if (decodedSize < outputSize)
            {
                // Not sure whether this should be an exception or not
                Log.Warning("Oodle decompression just decompressed {0} bytes of the expected {1} bytes", decodedSize, outputSize);
            }
        }

        public enum OodleLZ_Decode_ThreadPhase
        {
            ThreadPhase1 = 1,
            ThreadPhase2 = 2,
            ThreadPhaseAll = 3,
            Unthreaded = ThreadPhaseAll,
        }

        public enum OodleLZ_Verbosity
        {
            None = 0,
            Minimal = 1,
            Some = 2,
            Lots = 3,
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int OodleLZ_Decompress(void* srcBuf, int srcSize, void* rawBuf, int rawSize,
            int fuzzSafe = 1, int checkCRC = 0, OodleLZ_Verbosity verbosity = OodleLZ_Verbosity.None,
            void* decBufBase = null, int decBufSize = 0, void* fpCallback = null, void* callbackUserData = null,
            void* decoderMemory = null, int decoderMemorySize = 0,
            OodleLZ_Decode_ThreadPhase threadPhase = OodleLZ_Decode_ThreadPhase.Unthreaded);

        public static async Task<bool> DownloadOodleDll(string? path = null)
        {
            if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
            {
                Log.Warning("Cannot download Oodle library for non-64-bit windows or non-windows");
                return false;
            }

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            try
            {
                using var indexResponse = await client.GetAsync(WARFRAME_INDEX_URL).ConfigureAwait(false);
                await using var indexLzmaStream = await indexResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
                await using var indexStream = new MemoryStream();

                Lzma.Decompress(indexLzmaStream, indexStream);
                indexStream.Position = 0;

                string? dllUrl = null;
                using var indexReader = new StreamReader(indexStream);
                while (!indexReader.EndOfStream)
                {
                    var line = await indexReader.ReadLineAsync().ConfigureAwait(false);
                    if (string.IsNullOrEmpty(line)) continue;

                    if (line.Contains("Oodle/x64/final/oo2core_"))
                    {
                        dllUrl = WARFRAME_CONTENT_HOST + line[..line.IndexOf(',')];
                        break;
                    }
                }

                if (dllUrl == null)
                {
                    Log.Warning("Warframe index did not contain oodle dll");
                    return false;
                }

                var dllName = dllUrl[(dllUrl.LastIndexOf('/') + 1)..];
                dllName = dllName[..dllName.IndexOf('.')] + ".dll";

                using var dllResponse = await client.GetAsync(dllUrl).ConfigureAwait(false);
                await using var dllLzmaStream = await dllResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
                await using var dllStream = new MemoryStream();

                Lzma.Decompress(dllLzmaStream, dllStream);
                dllStream.Position = 0;
                var dllPath = path ?? dllName;
                var dllFs = File.Create(dllPath);
                await dllStream.CopyToAsync(dllFs).ConfigureAwait(false);
                await dllFs.DisposeAsync().ConfigureAwait(false);
                Log.Information($"Successfully downloaded oodle dll at \"{dllPath}\"");
                return true;
            }
            catch (Exception e)
            {
                Log.Warning(e, "Uncaught exception while downloading oodle dll");
            }

            return false;
        }
    }
}
