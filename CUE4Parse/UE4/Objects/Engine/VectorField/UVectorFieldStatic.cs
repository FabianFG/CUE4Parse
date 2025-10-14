using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.Engine.VectorField;

public class UVectorFieldStatic : UVectorField
{
    public FByteBulkData SourceData;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        
        SourceData = new FByteBulkData(Ar);
    }
}