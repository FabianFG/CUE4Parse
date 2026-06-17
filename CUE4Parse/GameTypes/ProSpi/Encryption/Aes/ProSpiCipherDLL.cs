using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using CUE4Parse.UE4.VirtualFileSystem;

namespace CUE4Parse.GameTypes.ProSpi.Encryption.Aes;

public static partial class ProSpiEncryption
{
    private const string ProSpiDecryptorDllName = "ProSpiDecryptor";

    private static readonly object _cipherDllLock = new();
    private static bool _cipherDllLoadAttempted;
    private static ProSpiDllDecryptDelegate? _cipherDecrypt;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private delegate int ProSpiDllDecryptDelegate(
        ulong descriptor,
        [MarshalAs(UnmanagedType.LPWStr)] string modulePath,
        [In] byte[] aesKey,
        int aesKeySize,
        [In] byte[] trailer,
        [In, Out] byte[] payload,
        int payloadSize
    );

    private static bool TryDllDecrypt(byte[] input, out byte[] output)
    {
        output = [];
        if (input.Length < EncryptionDataTrailerSize || _aesKey.Length == 0)
            return false;

        var trailer = input.AsSpan(input.Length - EncryptionDataTrailerSize, EncryptionDataTrailerSize);
        if (!TryGetDescriptorKey(trailer, out var descriptor))
            return false;

        var decrypt = GetDllDecrypt();
        if (decrypt == null)
            return false;

        output = new byte[input.Length];
        Buffer.BlockCopy(input, 0, output, 0, input.Length);
        var payloadSize = input.Length - EncryptionDataTrailerSize;
        var trailerBytes = trailer.ToArray();
        var result = decrypt(descriptor, _exePath, _aesKey, _aesKey.Length, trailerBytes, output, payloadSize);
        if (result != 1)
        {
            Log.Error("ProSpi DLL decryption failed: descriptor=0x{Descriptor:X16}, modulePath=\"{ModulePath}\", result={Result}", descriptor, _exePath, result);
            output = [];
            return false;
        }

        return true;
    }

    private static ProSpiDllDecryptDelegate? GetDllDecrypt()
    {
        if (_cipherDllLoadAttempted)
            return _cipherDecrypt;

        lock (_cipherDllLock)
        {
            if (_cipherDllLoadAttempted)
                return _cipherDecrypt;

            _cipherDllLoadAttempted = true;
            if (!TryLoadCipherDll(out var handle, out var loadedPath))
            {
                Log.Warning("ProSpi DLL unavailable: {DllName}", ProSpiDecryptorDllName);
                return null;
            }

            if (!NativeLibrary.TryGetExport(handle, "ProSpiDecrypt", out var export))
            {
                Log.Warning("ProSpi DLL missing ProSpiDecrypt export: {Path}", loadedPath);
                NativeLibrary.Free(handle);
                return null;
            }

            _cipherDecrypt = Marshal.GetDelegateForFunctionPointer<ProSpiDllDecryptDelegate>(export);
            Log.Information("ProSpi DLL loaded: {Path}", loadedPath);
            return _cipherDecrypt;
        }
    }

    private static bool TryLoadCipherDll(out IntPtr handle, out string loadedPath)
    {
        handle = IntPtr.Zero;
        loadedPath = ProSpiDecryptorDllName;

        try
        {
            using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"CUE4Parse.Resources.{ProSpiDecryptorDllName}.dll.gz");
            if (resourceStream == null)
            {
                loadedPath = "";
                return false;
            }

            using var gzipStream = new GZipStream(resourceStream, CompressionMode.Decompress);
            using var memory = new MemoryStream();

            gzipStream.CopyTo(memory);
            var dllBytes = memory.ToArray();

            var cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CUE4Parse", "Native", ProSpiDecryptorDllName);
            Directory.CreateDirectory(cacheDir);

            var dllPath = Path.Combine(cacheDir, $"{ProSpiDecryptorDllName}.dll");
            if (!File.Exists(dllPath) || !File.ReadAllBytes(dllPath).SequenceEqual(dllBytes))
                File.WriteAllBytes(dllPath, dllBytes);

            if (NativeLibrary.TryLoad(dllPath, out handle))
            {
                loadedPath = dllPath;
                return true;
            }

            loadedPath = dllPath;
            return false;
        }
        catch (Exception ex)
        {
            loadedPath = ex.Message;
            return false;
        }
    }

    private static string GetExePathFromReader(IAesVfsReader reader)
    {
        if (string.IsNullOrEmpty(reader.Path))
            return string.Empty;

        var pakPath = Path.GetFullPath(reader.Path);
        var pakDirectory = Path.GetDirectoryName(pakPath);
        if (string.IsNullOrEmpty(pakDirectory))
            return string.Empty;

        var contentDirectory = Directory.GetParent(pakDirectory);
        if (contentDirectory == null || !string.Equals(contentDirectory.Name, "Content", StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        var gameRoot = contentDirectory.Parent;
        if (gameRoot == null)
            return string.Empty;

        if (!Directory.Exists(gameRoot.FullName))
            return string.Empty;

        foreach (var modulePath in Directory.EnumerateFiles(gameRoot.FullName, "*.exe", SearchOption.AllDirectories))
        {
            using var stream = File.OpenRead(modulePath);
            using var sha256 = SHA256.Create();

            var hash = Convert.ToHexString(sha256.ComputeHash(stream));

            // It will only work if exe it was designed for exists
            if (string.Equals(hash, ExpectedExeHash, StringComparison.OrdinalIgnoreCase))
                return modulePath;
        }

        return string.Empty;
    }
}
