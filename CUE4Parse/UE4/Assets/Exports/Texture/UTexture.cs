using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.UE4.Assets.Exports.Texture
{
    public abstract class UTexture : UUnrealMaterial
    {
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