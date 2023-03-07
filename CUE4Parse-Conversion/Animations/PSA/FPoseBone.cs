using System;
using System.Diagnostics;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Animations.PSA
{
    public class FPoseBone : ICloneable
    {
        public FTransform Transform;
        public int ParentIndex;
        public string Name;
        public bool IsValidKey;
        public bool Accumulated;

        public FPoseBone()
        {
            Transform = FTransform.Identity;
            ParentIndex = -1;
            Name = "";
            IsValidKey = false;
            Accumulated = false;
        }

        public void AccumulateWithAdditiveScale(FTransform atom, float weight)
        {
            Debug.Assert(!Accumulated);
            Transform.Rotation = atom.Rotation * weight * Transform.Rotation;
            Transform.Translation += atom.Translation * weight;
            Transform.Scale3D *= FVector.OneVector + atom.Scale3D * weight;
            Accumulated = true;
        }

        public object Clone()
        {
            return new FPoseBone
            {
                Transform = (FTransform) Transform.Clone(),
                ParentIndex = ParentIndex,
                Name = Name,
                IsValidKey = IsValidKey,
                Accumulated = Accumulated
            };
        }
    }
}
