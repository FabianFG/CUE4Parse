using System;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Serilog;

namespace CUE4Parse_Conversion.Textures.BC;

public static class DetexHelper
{
    private const string MANIFEST_URL = "CUE4Parse_Conversion.Resources.Detex.dll";
    public const string DLL_NAME = "Detex.dll";

    private static Detex? Instance { get; set; }

    /// <summary>
    /// Initializes the Detex library with a given path.
    /// </summary>
    public static void Initialize(string path)
    {
        Instance?.Dispose();
        if (File.Exists(path))
            Instance = new Detex(path);
    }

    /// <summary>
    /// Initializes Detex with a pre-existing instance.
    /// </summary>
    public static void Initialize(Detex instance)
    {
        Instance?.Dispose();
        Instance = instance;
    }

    /// <summary>
    /// Load the Detex library DLL.
    /// </summary>
    public static bool LoadDll(string? path = null)
    {
        if (File.Exists(path ?? DLL_NAME))
            return true;
        return LoadDllAsync(path).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Decode the encoded data using the Detex library.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] DecodeDetexLinear(byte[] inp, int width, int height, bool isFloat, DetexTextureFormat inputFormat, DetexPixelFormat outputPixelFormat)
    {
        if (Instance is null)
        {
            const string message = "Detex decompression failed: not initialized";
            throw new Exception(message);
        }

        var dst = new byte[width * height * (isFloat ? 16 : 4)];
        Instance.DecodeDetexLinear(inp, dst, width, height, inputFormat, outputPixelFormat);
        return dst;
    }

    /// <summary>
    /// Asynchronously loads the Detex DLL from resources.
    /// </summary>
    public static async Task<bool> LoadDllAsync(string? path)
    {
        try
        {
            var dllPath = path ?? DLL_NAME;

            if (File.Exists(dllPath))
            {
                Log.Information($"Detex DLL already exists at \"{dllPath}\".");
                return true;
            }

            await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(MANIFEST_URL);
            if (stream == null)
            {
                throw new MissingManifestResourceException("Couldn't find Detex.dll in Embedded Resources.");
            }

            await using var dllFs = File.Create(dllPath);
            await stream.CopyToAsync(dllFs).ConfigureAwait(false);

            Log.Information($"Successfully loaded Detex DLL from embedded resources to \"{dllPath}\"");
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Uncaught exception while loading Detex DLL");
            return false;
        }
    }

}
