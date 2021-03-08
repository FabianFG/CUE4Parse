using System.Collections.Generic;

namespace CUE4Parse.UE4.Assets.Exports.Materials
{
    public abstract class UUnrealMaterial : UObject
    {
        public virtual bool IsTextureCube { get; } = false;

        public abstract void GetParams(CMaterialParams parameters);

        public virtual void AppendReferencedTextures(IList<UUnrealMaterial> outTextures, bool onlyRendered)
        {
            var parameters = new CMaterialParams();
            GetParams(parameters);
            parameters.AppendAllTextures(outTextures);
        }
    }
}