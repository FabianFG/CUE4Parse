using System;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.Meshes.PSK
{
    public class CSkelMeshBone: ICloneable
    {
        public FName Name;
        public int ParentIndex;
        public FVector Position;
        public FQuat Orientation;

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}