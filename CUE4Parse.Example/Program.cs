using System;
using System.Collections.Generic;
using System.IO;
using CUE4Parse.Compression;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace CUE4Parse.Example
{
    // ---------------------------------------------------------------------------------------------
    // Verification harness for Wuthering Waves "OodleTextureStorageProviderFactory" textures.
    //
    // Goal: figure out how the mip chain stored inside UOodleTextureStorageProviderFactory is
    // compressed, so we can implement proper texture export.
    //
    // HOW TO RUN (Windows, so the native Oodle dll can be downloaded/loaded):
    //   1. Put the sample .uasset/.uexp files in a folder.
    //   2. Set SampleDir below (or pass the folder as the first command-line arg).
    //   3. dotnet run --project CUE4Parse.Example -c Release
    //
    // The harness will, for each texture:
    //   * parse the factory (SizeX/SizeY, compressed payload size, raw header ints)
    //   * report whether the following Texture2D now parses (format + mips)
    //   * try to Oodle-decompress the payload at several candidate uncompressed sizes
    //     (full BC7 mip chain, top mip only, etc.) and report which one succeeds.
    // ---------------------------------------------------------------------------------------------
    public static class Program
    {
        // EDIT THIS (or pass as args[0]):
        private const string SampleDir = @"C:\uaue";

        private const EGame Game = EGame.GAME_WutheringWaves;

        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
                .CreateLogger();

            var dir = args.Length > 0 ? args[0] : SampleDir;
            if (!Directory.Exists(dir))
            {
                Console.WriteLine($"Sample dir not found: {dir}");
                Console.WriteLine("Set SampleDir in Program.cs or pass the folder as the first argument.");
                return;
            }

            // Native Oodle: on Windows this downloads oodle-data-shared.dll from the OodleUE release.
            try
            {
                OodleHelper.Initialize();
                Console.WriteLine(OodleHelper.Instance is null
                    ? "!! Oodle NOT initialized (decompression tests will be skipped)"
                    : "Oodle initialized OK");
            }
            catch (Exception e)
            {
                Console.WriteLine($"!! Oodle init failed: {e.Message}");
            }

            var provider = new DefaultFileProvider(dir, SearchOption.TopDirectoryOnly, new VersionContainer(Game));
            provider.Initialize();

            foreach (var file in Directory.GetFiles(dir, "*.uasset"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                Console.WriteLine($"\n############################## {name} ##############################");
                try
                {
                    var exports = provider.LoadPackage(name + ".uasset").GetExports();

                    UOodleTextureStorageProviderFactory? factory = null;
                    UTexture2D? texture = null;
                    foreach (var e in exports)
                    {
                        if (e is UOodleTextureStorageProviderFactory f) factory = f;
                        if (e is UTexture2D t) texture = t;
                    }

                    if (factory is null)
                    {
                        Console.WriteLine("No OodleTextureStorageProviderFactory export found.");
                        continue;
                    }

                    Console.WriteLine($"Factory: SizeX={factory.SizeX} SizeY={factory.SizeY} " +
                                      $"flags=0x{factory.BulkDataFlags:X} ElementCount={factory.ElementCount} " +
                                      $"SizeOnDisk={factory.SizeOnDisk} payload={factory.CompressedData.Length} bytes");
                    Console.WriteLine("HeaderInts: " + string.Join(", ", factory.HeaderInts));

                    if (texture is not null)
                    {
                        Console.WriteLine($"Texture2D: Format={texture.Format} " +
                                          $"PlatformSize={texture.PlatformData.SizeX}x{texture.PlatformData.SizeY} " +
                                          $"Mips={texture.PlatformData.Mips.Length}");
                        for (var i = 0; i < texture.PlatformData.Mips.Length; i++)
                        {
                            var m = texture.PlatformData.Mips[i];
                            Console.WriteLine($"  mip[{i}] {m.SizeX}x{m.SizeY} bulk={(m.BulkData?.Data?.Length ?? -1)}");
                        }
                    }

                    TryDecompress(factory);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FAILED: {ex}");
                }
            }
        }

        private static void TryDecompress(UOodleTextureStorageProviderFactory factory)
        {
            if (OodleHelper.Instance is null || factory.CompressedData.Length == 0)
                return;

            Console.WriteLine("Payload first 32 bytes: " + Convert.ToHexString(factory.CompressedData, 0, Math.Min(32, factory.CompressedData.Length)));

            var candidates = new List<(string label, int size)>();

            // BC7 = 16 bytes per 4x4 block.
            int Bc7(int w, int h) => Math.Max(1, (w + 3) / 4) * Math.Max(1, (h + 3) / 4) * 16;

            var full = 0;
            {
                int w = factory.SizeX, h = factory.SizeY;
                while (true)
                {
                    full += Bc7(w, h);
                    if (w <= 1 && h <= 1) break;
                    w = Math.Max(1, w / 2);
                    h = Math.Max(1, h / 2);
                }
            }
            candidates.Add(("BC7 full mip chain", full));
            candidates.Add(("BC7 top mip only", Bc7(factory.SizeX, factory.SizeY)));

            // A few defensive extras in case block rounding / alignment differs.
            candidates.Add(("SizeX*SizeY (1 byte/px)", factory.SizeX * factory.SizeY));
            candidates.Add(("SizeX*SizeY*4 (RGBA)", factory.SizeX * factory.SizeY * 4));

            foreach (var (label, size) in candidates)
            {
                if (size <= 0) continue;
                try
                {
                    var dst = new byte[size];
                    var decoded = OodleHelper.Instance.Decompress(factory.CompressedData, dst);
                    var ok = decoded == size;
                    Console.WriteLine($"  [{(ok ? "MATCH" : "    ")}] {label}: dst={size} -> decoded={decoded}" +
                                      (decoded > 0 ? $"  first16={Convert.ToHexString(dst, 0, Math.Min(16, (int) decoded))}" : ""));
                    if (ok)
                    {
                        var outPath = Path.Combine(Path.GetTempPath(), $"decoded_{size}.bc7.bin");
                        File.WriteAllBytes(outPath, dst);
                        Console.WriteLine($"        -> wrote decoded payload to {outPath}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"  [FAIL ] {label}: dst={size} -> {e.Message}");
                }
            }
        }
    }
}
