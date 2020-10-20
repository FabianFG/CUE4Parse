using CUE4Parse.UE4.Assets.Exports.Materials;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Textures
{
    public abstract class UTexture : UUnrealMaterial
    {
        protected UTexture() { }
        protected UTexture(FObjectExport exportObject) : base(exportObject) { }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            var stripFlags = Ar.Read<FStripDataFlags>();
            
            // Legacy serialization.
            /*
            if (!StripFlags.IsEditorDataStripped())
            {
                throw new NotImplementedException("Non-Cooked Textures are not supported");    
            }
            */
        }
    }
}