using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Materials
{
    public abstract class UUnrealMaterial : UObject
    {
        protected UUnrealMaterial() { }
        protected UUnrealMaterial(FObjectExport exportObject) : base(exportObject) { }

        public bool IsTextureCube { get; } = false;
        
        // TODO abstract fun getParams(CMaterialParams params)
        // TODO open fun appendReferencedTextures(outTextures: MutableList<UUnrealMaterial>, onlyRendered: Boolean) 
    }
}