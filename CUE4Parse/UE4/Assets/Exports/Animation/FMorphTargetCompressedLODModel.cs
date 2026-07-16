namespace CUE4Parse.UE4.Assets.Exports.Animation;

public class FMorphTargetCompressedLODModel(
    FDeltaBatchHeader[] packedDeltaHeaders,
    uint[] packedDeltaData,
    float positionPrecision,
    float tangentPrecision)
{
    public FDeltaBatchHeader[] PackedDeltaHeaders = packedDeltaHeaders;
    public uint[] PackedDeltaData = packedDeltaData;
    public float PositionPrecision = positionPrecision;
    public float TangentPrecision = tangentPrecision;
}