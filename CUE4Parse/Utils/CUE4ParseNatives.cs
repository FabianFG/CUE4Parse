using System;
using System.Runtime.InteropServices;

namespace CUE4Parse.Utils; 

public static class CUE4ParseNatives 
{
    [DllImport("CUE4Parse-Natives", CallingConvention = CallingConvention.Cdecl, EntryPoint = "IsFeatureAvailable")]
    private static extern bool _IsFeatureAvailable([MarshalAs(UnmanagedType.LPUTF8Str)] string featureName);

    public static bool IsFeatureAvailable(string featureName) {
        try {
            return _IsFeatureAvailable(featureName);
        }
        catch (DllNotFoundException _) {
            return false;
        }
    }
}