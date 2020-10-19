using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FBoneNode : IUStruct
    {
        public readonly EBoneTranslationRetargetingMode TranslationRetargetingMode;
    }
}
