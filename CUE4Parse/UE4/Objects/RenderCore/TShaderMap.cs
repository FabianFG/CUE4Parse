using CUE4Parse.UE4.Assets.Exports.Material;

namespace CUE4Parse.UE4.Objects.RenderCore;

public class TShaderMap<InShaderMapType, InPointerTableType> : FShaderMapBase where InShaderMapType : FShaderMapContent, new() where InPointerTableType : FPointerTableBase, new()
{
    public TShaderMap()
    {
        Content = new InShaderMapType();
        PointerTable = new InPointerTableType();
    }
}