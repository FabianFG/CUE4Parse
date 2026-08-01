using System.Runtime.InteropServices;
using CUE4Parse.Utils;

namespace CUE4Parse_Conversion.UEFormat.Natives;

public static class UEFormatNative
{
    public const string LibraryName = CUE4ParseNatives.LibraryName;

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern UEFormatBufferResult ueformat_save_model(
        in UEFormatModelDesc model,
        in UEFormatSaveOptions options);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern UEFormatBufferResult ueformat_save_anim(
        in UEFormatAnimDesc anim,
        in UEFormatSaveOptions options);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern UEFormatBufferResult ueformat_save_pose(
        in UEFormatPoseDesc pose,
        in UEFormatSaveOptions options);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ueformat_buffer_free(ref UEFormatBuffer buffer);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ueformat_status_string(UEFormatStatus status);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte ueformat_latest_version();

    public static void EnsureAvailable()
    {
        if (!CUE4ParseNatives.IsFeatureAvailable("UEFormat"))
        {
            throw new InvalidOperationException(
                "UEFormat native support is unavailable. Ensure CUE4Parse-Natives was built with the UEFormat submodule.");
        }
    }
}
