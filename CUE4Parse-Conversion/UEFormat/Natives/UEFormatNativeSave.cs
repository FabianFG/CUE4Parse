using System.Runtime.InteropServices;
using CUE4Parse_Conversion.UEFormat.Enums;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;

namespace CUE4Parse_Conversion.UEFormat.Natives;

internal static class UEFormatNativeSave
{
    public static byte[] SaveModel(ref UEFormatModelDesc model, string objectName, string? objectPath, ExporterOptions options, NativePinScope pin)
    {
        UEFormatNative.EnsureAvailable();
        var saveOptions = CreateSaveOptions(objectName, objectPath, options, pin);
        var result = UEFormatNative.ueformat_save_model(in model, in saveOptions);
        return ConsumeBuffer(result);
    }

    public static byte[] SaveAnim(ref UEFormatAnimDesc anim, string objectName, string? objectPath, ExporterOptions options, NativePinScope pin)
    {
        UEFormatNative.EnsureAvailable();
        var saveOptions = CreateSaveOptions(objectName, objectPath, options, pin);
        var result = UEFormatNative.ueformat_save_anim(in anim, in saveOptions);
        return ConsumeBuffer(result);
    }

    public static byte[] SavePose(ref UEFormatPoseDesc pose, string objectName, string? objectPath, ExporterOptions options, NativePinScope pin)
    {
        UEFormatNative.EnsureAvailable();
        var saveOptions = CreateSaveOptions(objectName, objectPath, options, pin);
        var result = UEFormatNative.ueformat_save_pose(in pose, in saveOptions);
        return ConsumeBuffer(result);
    }

    private static UEFormatSaveOptions CreateSaveOptions(string objectName, string? objectPath, ExporterOptions options, NativePinScope pin)
    {
        return new UEFormatSaveOptions
        {
            ObjectName = pin.AllocUtf8(objectName),
            ObjectPath = pin.AllocUtf8(objectPath ?? string.Empty),
            Compression = (byte) MapCompression(options.CompressionFormat),
            CompressionLevel = 0,
        };
    }

    private static UEFormatCompression MapCompression(EFileCompressionFormat format) => format switch
    {
        EFileCompressionFormat.GZIP => UEFormatCompression.Gzip,
        EFileCompressionFormat.ZSTD => UEFormatCompression.Zstd,
        _ => UEFormatCompression.None,
    };

    private static byte[] ConsumeBuffer(UEFormatBufferResult result)
    {
        if (result.Status != UEFormatStatus.Ok || result.Buffer.Data == IntPtr.Zero)
        {
            var message = result.Error != IntPtr.Zero
                ? Marshal.PtrToStringUTF8(result.Error)
                : Marshal.PtrToStringUTF8(UEFormatNative.ueformat_status_string(result.Status));
            throw new InvalidOperationException($"UEFormat native save failed: {message ?? result.Status.ToString()}");
        }

        try
        {
            var size = checked((int) result.Buffer.Size);
            var bytes = new byte[size];
            if (size > 0)
                Marshal.Copy(result.Buffer.Data, bytes, 0, size);
            return bytes;
        }
        finally
        {
            var buffer = result.Buffer;
            UEFormatNative.ueformat_buffer_free(ref buffer);
        }
    }

    public static UEFormatVector ToVector(FVector v) => new(v.X, v.Y, v.Z);

    public static UEFormatQuat ToQuat(FQuat q) => new(q.X, q.Y, q.Z, q.W);

    public static UEFormatColor ToColor(FColor c) => new() { R = c.R, G = c.G, B = c.B, A = c.A };

    public static UEFormatMeshUV ToUv(FMeshUVFloat uv) => new() { U = uv.U, V = uv.V };
}
