using System.Collections.Generic;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Materials
{
    public abstract class UUnrealMaterial : UObject
    {
        protected UUnrealMaterial() { }
        protected UUnrealMaterial(FObjectExport exportObject) : base(exportObject) { }

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