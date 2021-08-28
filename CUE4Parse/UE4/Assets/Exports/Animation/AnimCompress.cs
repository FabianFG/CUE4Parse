namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public class UAnimCompress : UAnimBoneCompressionCodec
    {
        public override ICompressedAnimData AllocateAnimData() => new FUECompressedAnimData();
    }
}