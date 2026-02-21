namespace CUE4Parse.UE4.Assets.Exports.Animation;

public class UAnimCompress : UAnimBoneCompressionCodec
{
    public override ICompressedAnimData AllocateAnimData() => new FUECompressedAnimData();
}

public class UAnimCompress_BitwiseCompressOnly : UAnimCompress;
public class UAnimCompress_PerTrackCompression : UAnimCompress;
public class UAnimCompress_RemoveLinearKeys : UAnimCompress;
public class UAnimCompress_RemoveEverySecondKey : UAnimCompress;
public class UAnimCompress_Constant : UAnimCompress;
public class UAnimCompress_LeastDestructive : UAnimCompress;
