using System;
using System.Runtime.InteropServices;

namespace CUE4Parse.Utils; 

public static class CUE4ParseNatives 
{
    public const string LibraryName = "CUE4Parse-Natives";
    
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "IsFeatureAvailable")]
    private static extern bool _IsFeatureAvailable([MarshalAs(UnmanagedType.LPStr)] string featureName);

    public static bool IsFeatureAvailable(string featureName) {
        try {
            return _IsFeatureAvailable(featureName);
        }
        catch (DllNotFoundException _) {
            return false;
        }
    }
}