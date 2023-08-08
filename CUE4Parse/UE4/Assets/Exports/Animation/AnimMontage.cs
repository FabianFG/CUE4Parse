using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public class UAnimMontage : UAnimCompositeBase
    {
        public FCompositeSection[] CompositeSections;
        public FSlotAnimationTrack[] SlotAnimTracks;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            CompositeSections = GetOrDefault(nameof(CompositeSections), Array.Empty<FCompositeSection>());
            SlotAnimTracks = GetOrDefault(nameof(SlotAnimTracks), Array.Empty<FSlotAnimationTrack>());
        }

        public float CalculateSequenceLength()
        {
            float calculatedSequenceLength = 0.0f;
            foreach (var slotAnimTrack in SlotAnimTracks)
            {
                if (slotAnimTrack.AnimTrack.AnimSegments.Length > 0)
                {
                    calculatedSequenceLength = Math.Max(calculatedSequenceLength, slotAnimTrack.AnimTrack.GetLength());
                }
            }
            return calculatedSequenceLength;
        }
    }

    [StructFallback]
    public class FCompositeSection : FAnimLinkableElement
    {
        public FName SectionName;
        public FName NextSectionName;
        public UAnimMetaData[] MetaData;

        public FCompositeSection(FStructFallback fallback) : base(fallback)
        {
            SectionName = fallback.GetOrDefault<FName>(nameof(SectionName));
            NextSectionName = fallback.GetOrDefault<FName>(nameof(NextSectionName));
            MetaData = fallback.GetOrDefault<UAnimMetaData[]>(nameof(MetaData));
        }
    }

    [StructFallback]
    public class FSlotAnimationTrack
    {
        public FName SlotName;
        public FAnimTrack AnimTrack;

        public FSlotAnimationTrack(FStructFallback fallback)
        {
            SlotName = fallback.GetOrDefault<FName>(nameof(SlotName));
            AnimTrack = fallback.GetOrDefault<FAnimTrack>(nameof(AnimTrack));
        }
    }
}
