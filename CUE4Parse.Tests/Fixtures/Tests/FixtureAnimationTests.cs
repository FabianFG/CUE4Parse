using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Objects.Engine.Animation;
using static CUE4Parse.Tests.Fixtures.FixtureTestUtilities;

namespace CUE4Parse.Tests.Fixtures;

public class FixtureAnimationTests
{
    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void SkeletonPreservesHierarchyAndSocket(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var skeleton = LoadExport<USkeleton>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Animations/SKEL_Fixture.uasset",
            "SKEL_Fixture");

        AssertReferenceSkeleton(skeleton.ReferenceSkeleton);
        Assert.Equal(3, skeleton.BoneCount);

        var socket = Assert.IsType<USkeletalMeshSocket>(Assert.Single(skeleton.Sockets).Load<USkeletalMeshSocket>());
        Assert.Equal("FixtureSocket", socket.SocketName.Text);
        Assert.Equal("Joint_1", socket.BoneName.Text);
        AssertVector(socket.RelativeLocation, 2.5f, -5.0f, 7.5f);
        Assert.Equal((5.0f, 15.0f, 25.0f),
            (socket.RelativeRotation.Pitch, socket.RelativeRotation.Yaw, socket.RelativeRotation.Roll));
        AssertVector(socket.RelativeScale, 1.0f, 1.25f, 0.75f);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void AnimationSequencePreservesCompressedTracksAndCurve(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var sequence = LoadExport<UAnimSequence>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Animations/AS_Fixture.uasset",
            "AS_Fixture");

        Assert.Equal(1.0f, sequence.SequenceLength);
        Assert.Equal(1.25f, sequence.RateScale);
        Assert.True(sequence.Skeleton.TryLoad<USkeleton>(out var skeleton));
        Assert.Equal("SKEL_Fixture", skeleton.Name);
        Assert.Equal([0, 1, 2],
            sequence.CompressedTrackToSkeletonMapTable.Select(track => track.BoneTreeIndex).ToArray());
        Assert.Equal("FixtureCurve", Assert.Single(sequence.CompressedCurveNames!).DisplayName.Text,
            ignoreCase: true);
        Assert.NotEmpty(sequence.CompressedCurveByteStream!);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void AnimationMontagePreservesSlotSegmentAndSections(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var montage = LoadExport<UAnimMontage>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Animations/AM_Fixture.uasset",
            "AM_Fixture");

        var slot = Assert.Single(montage.SlotAnimTracks);
        Assert.Equal("FixtureSlot", slot.SlotName.Text);
        var segment = Assert.Single(slot.AnimTrack.AnimSegments);
        Assert.Equal("AS_Fixture", Assert.IsType<UAnimSequence>(segment.AnimReference.Load<UAnimSequence>()).Name);
        Assert.Equal(0.0f, segment.StartPos);
        Assert.Equal(0.0f, segment.AnimStartTime);
        Assert.Equal(1.0f, segment.AnimEndTime);
        Assert.Equal(1.25f, segment.AnimPlayRate);
        Assert.Equal(2, segment.LoopingCount);
        Assert.Equal(1.28f, montage.CalculateSequenceLength(), precision: 5);

        Assert.Equal(["Intro", "Loop"],
            montage.CompositeSections.Select(section => section.SectionName.Text).ToArray());
        Assert.Equal(["Loop", "Loop"],
            montage.CompositeSections.Select(section => section.NextSectionName.Text).ToArray());
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void AnimationCompositePreservesOrderedSegmentsAndPlayback(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var composite = LoadExport<UAnimComposite>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Animations/AC_Fixture.uasset",
            "AC_Fixture");

        Assert.Equal(2, composite.AnimationTrack.AnimSegments.Length);
        var first = composite.AnimationTrack.AnimSegments[0];
        var second = composite.AnimationTrack.AnimSegments[1];
        Assert.All(composite.AnimationTrack.AnimSegments, segment =>
            Assert.Equal("AS_Fixture", Assert.IsType<UAnimSequence>(segment.AnimReference.Load()).Name));
        Assert.Equal((0f, 1f, 1f, 1),
            (first.AnimStartTime, first.AnimEndTime, first.AnimPlayRate, first.LoopingCount));
        Assert.Equal((0f, 1f, 0.5f, 2),
            (second.AnimStartTime, second.AnimEndTime, second.AnimPlayRate, second.LoopingCount));
        Assert.Equal(first.GetLength(), second.StartPos, precision: 5);
        Assert.Equal(composite.SequenceLength, composite.AnimationTrack.GetLength(), precision: 5);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void BlendSpacePreservesSamples(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var blendSpace = LoadExport<UBlendSpace>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Animations/BS_Fixture.uasset",
            "BS_Fixture");

        Assert.Equal(1.0f, blendSpace.AnimLength);
        Assert.Equal(3, blendSpace.SampleData.Length);
        AssertVector(blendSpace.SampleData[0].SampleValue, 0.0f, 0.0f, 0.0f);
        AssertVector(blendSpace.SampleData[1].SampleValue, 50.0f, 25.0f, 0.0f);
        AssertVector(blendSpace.SampleData[2].SampleValue, 100.0f, 100.0f, 0.0f);
        Assert.All(blendSpace.SampleData, sample =>
        {
            Assert.Equal(1.0f, sample.RateScale);
            Assert.Equal("AS_Fixture", Assert.IsType<UAnimSequence>(sample.Animation?.Load<UAnimSequence>()).Name);
        });
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void PoseAssetPreservesPosesTracksAndCurve(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var poseAsset = LoadExport<UPoseAsset>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Animations/PA_Fixture.uasset",
            "PA_Fixture");

        Assert.Equal(Enumerable.Range(0, 31).Select(index => $"Pose_{index}"),
            poseAsset.PoseContainer.GetPoseNames());
        Assert.Equal(["Root", "Joint_1", "Joint_2"],
            poseAsset.PoseContainer.Tracks.Select(track => track.Text).ToArray());
        Assert.Equal(31, poseAsset.PoseContainer.Poses?.Length);
        Assert.Equal("FixtureCurve", Assert.Single(poseAsset.PoseContainer.Curves).CurveName.Text);
        Assert.Equal(3, poseAsset.PoseContainer.TrackPoseInfluenceIndices.Length);
        Assert.All(poseAsset.PoseContainer.Poses!, pose =>
            Assert.InRange(pose.LocalSpacePose.Length, 2, poseAsset.PoseContainer.Tracks.Length));
        Assert.Contains(poseAsset.PoseContainer.Poses!, pose =>
            pose.LocalSpacePose.Length == poseAsset.PoseContainer.Tracks.Length);
    }
}
