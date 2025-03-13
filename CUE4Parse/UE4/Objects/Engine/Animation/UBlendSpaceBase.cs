using System;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.Engine.Animation
{
    public class UBlendSpaceBase : UAnimationAsset
    {
        /// <summary>
        /// This is the maximum length of any sample in the blendspace.
        /// </summary>
        public float AnimLength;

        /// <summary>
        /// Sample animation data
        /// </summary>
        public FBlendSample[] SampleData;

        /// <summary>
        /// Grid samples, indexing scheme imposed by subclass
        /// </summary>
        public FEditorElement[] GridSamples;

        /// <summary>
        /// Blend Parameters for each axis.
        /// </summary>
        public FBlendParameter[] BlendParameters;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            AnimLength = GetOrDefault(nameof(AnimLength), 0.0f);
            SampleData = GetOrDefault(nameof(SampleData), Array.Empty<FBlendSample>());
            GridSamples = GetOrDefault(nameof(GridSamples), Array.Empty<FEditorElement>());
            TryGetAllValues(out BlendParameters, nameof(BlendParameters));
        }
    }
}
