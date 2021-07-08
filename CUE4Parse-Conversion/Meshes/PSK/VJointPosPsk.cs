using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse_Conversion.Meshes.Common;

namespace CUE4Parse_Conversion.Meshes.PSK
{
    public class VJointPosPsk
    {
        public FQuat Orientation;
        public FVector Position;
        public float Length;
        public FVector Size;
        
        public void Serialize(FCustomArchiveWriter writer)
        {
            Orientation.Serialize(writer);
            Position.Serialize(writer);
            writer.Write(Length);
            Size.Serialize(writer);
        }
    }
}