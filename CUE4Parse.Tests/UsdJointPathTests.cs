using System.Text;
using System.Text.RegularExpressions;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Writers.USD;

namespace CUE4Parse.Tests;

/// <summary>
/// Joint paths have to be legal SdfPaths. UE bone names are under no such obligation.
/// </summary>
/// <remarks>
/// Found on shipped content: skeletons authored in 3ds Max Biped carry names like
/// <c>Bip001-L-Clavicle</c>. Writing those into the joints array yields ill-formed paths, and
/// Blender responds by dropping the entire skeleton — mesh, materials and textures all import,
/// the armature comes in empty, and nothing raises. Measured on two titles: 16 of 456 skeletons
/// in one, 1 of 1,436 in the other.
///
/// So what is pinned here is not "is something being substituted" but that the substitution
/// cannot produce the other half of that same error message. The "duplicated" in Blender's
/// warning is reachable: a real skeleton carries <c>steering-wheel</c> and <c>steering_wheel</c>
/// as siblings.
/// </remarks>
public class UsdJointPathTests
{
    private static readonly Regex LegalIdentifier = new(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    /// <summary>(name, parent index) pairs into an exportable skeleton. Parents precede children,
    /// as they do in a UE reference skeleton.</summary>
    private static SkeletonDto Skeleton(params (string Name, int Parent)[] bones)
    {
        var info = bones.Select(b => new FMeshBoneInfo(new FName(b.Name), b.Parent)).ToArray();
        var pose = bones.Select(_ => new FTransform(FQuat.Identity, FVector.ZeroVector, FVector.OneVector)).ToArray();

        return new SkeletonDto(new USkeleton
        {
            Name = "TestSkeleton",
            ReferenceSkeleton = new FReferenceSkeleton(info, pose),
        });
    }

    private static string[] JointsOf(UsdPrim skelRoot) => skelRoot.Children
        .SelectMany(c => c.Properties)
        .OfType<UsdAttribute>()
        .Single(a => a.Name == "joints")
        .Value.AsValues()
        .Select(v => (string) v.RawValue!)
        .ToArray();

    /// <summary>Every segment has to be legal, not just the string as a whole.</summary>
    private static void AssertEveryPathIsLegal(IEnumerable<string> paths)
    {
        foreach (var path in paths)
            foreach (var segment in path.Split('/'))
                Assert.Matches(LegalIdentifier, segment);
    }

    private static string Serialize(SkeletonDto dto) =>
        Encoding.UTF8.GetString(new UsdStage(dto.ToSkelRoot()).SerializeToBinary());

    [Fact]
    public void BipedBoneNamesBecomeLegalPathsInsteadOfPathsPixarRefusesToParse()
    {
        // The first four bones of a real Biped skeleton.
        var joints = JointsOf(Skeleton(
            ("Root", -1),
            ("Bip001", 0),
            ("Bip001-Pelvis", 1),
            ("Bip001-Spine", 2)).ToSkelRoot());

        AssertEveryPathIsLegal(joints);
        Assert.Equal(
        [
            "Root",
            "Root/Bip001",
            "Root/Bip001/Bip001_Pelvis",
            "Root/Bip001/Bip001_Pelvis/Bip001_Spine",
        ], joints);
    }

    [Fact]
    public void SiblingsThatSanitiseOntoEachOtherGetDistinctPaths()
    {
        // Taken from a shipped vehicle skeleton. Substituting without disambiguating turns
        // "invalid" into "duplicated", which is rejected on identical grounds — the bug would
        // survive the fix with its symptoms unchanged.
        var joints = JointsOf(Skeleton(
            ("root", -1),
            ("steering-wheel", 0),
            ("steering_wheel", 0)).ToSkelRoot());

        AssertEveryPathIsLegal(joints);
        Assert.Equal(joints.Length, joints.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void TheBoneThatWasAlreadyLegalKeepsItsName()
    {
        // The cost of disambiguation belongs on the malformed name. First-come-first-served
        // would push a perfectly good steering_wheel to steering_wheel_1 because a sibling was
        // malformed, turning a 17-file fix into churn across thousands.
        var joints = JointsOf(Skeleton(
            ("root", -1),
            ("steering-wheel", 0),
            ("steering_wheel", 0)).ToSkelRoot());

        Assert.Equal("root/steering_wheel", joints[2]);
        Assert.NotEqual("root/steering_wheel", joints[1]);
    }

    [Fact]
    public void SkeletonsThatWereAlreadyValidComeOutUnchanged()
    {
        // The overwhelming majority. Normalisation should leave no trace on them.
        var joints = JointsOf(Skeleton(
            ("Root", -1),
            ("pelvis", 0),
            ("spine_01", 1),
            ("_leading_underscore", 1)).ToSkelRoot());

        Assert.Equal(
        [
            "Root",
            "Root/pelvis",
            "Root/pelvis/spine_01",
            "Root/pelvis/_leading_underscore",
        ], joints);
    }

    [Fact]
    public void NamesStartingWithADigitAreLegalisedToo()
    {
        // SdfPath rejects a segment beginning with a digit. UE is perfectly happy with one.
        var joints = JointsOf(Skeleton(("Root", -1), ("01_jaw", 0)).ToSkelRoot());

        AssertEveryPathIsLegal(joints);
        Assert.Equal("Root/_01_jaw", joints[1]);
    }

    [Fact]
    public void SameNameUnderDifferentParentsIsNotACollision()
    {
        // Common in mirrored rigs. Distinct paths do not conflict and must not be renamed.
        var joints = JointsOf(Skeleton(
            ("Root", -1),
            ("L", 0),
            ("R", 0),
            ("hand", 1),
            ("hand", 2)).ToSkelRoot());

        Assert.Equal("Root/L/hand", joints[3]);
        Assert.Equal("Root/R/hand", joints[4]);
    }

    [Fact]
    public void TwoSiblingsWithTheSameLegalNameStillGetDistinctPaths()
    {
        // Both names are legal, so an implementation that only touches malformed names lets
        // them through — but a duplicate path is rejected for exactly the reasons a malformed
        // one is. Being legal is not on its own a reason to emit a duplicate.
        var joints = JointsOf(Skeleton(
            ("Root", -1),
            ("hand", 0),
            ("hand", 0)).ToSkelRoot());

        AssertEveryPathIsLegal(joints);
        Assert.Equal(joints.Length, joints.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("Root/hand", joints[1]);
    }

    [Fact]
    public void RenamedSkeletonsRecordWhatTheEngineActuallyCalledTheBones()
    {
        // Renaming is lossy: Bip001-L-Clavicle and Bip001_L_Clavicle collapse onto one
        // identifier. Round-tripping back to UE needs the original spelling.
        var usda = Serialize(Skeleton(("Root", -1), ("Bip001-Pelvis", 0)));

        Assert.Contains("unrealJointNames", usda);
        Assert.Contains("\"Bip001-Pelvis\"", usda);
    }

    [Fact]
    public void UntouchedSkeletonsDoNotCarryTheOriginalNamesArray()
    {
        // On a skeleton nobody renamed it would only ever be a verbatim copy of the joints.
        var usda = Serialize(Skeleton(("Root", -1), ("pelvis", 0)));

        Assert.DoesNotContain("unrealJointNames", usda);
    }

    [Fact]
    public void TheJointsArrayIsWrittenOnceAndReusedRatherThanRecomputed()
    {
        // UsdAnimFormat reads joints back off the Skeleton prim to hand to SkelAnimation.
        // Pinning that path: were it ever to recompute them independently, any drift between
        // the two normalisations would silently bind the animation to the wrong bones.
        var root = Skeleton(("Root", -1), ("Bip001-Pelvis", 0), ("Bip001-Spine", 1)).ToSkelRoot();
        var fromSkeleton = root.Children[0].Properties.OfType<UsdAttribute>().Single(a => a.Name == "joints");

        Assert.Equal(JointsOf(root), fromSkeleton.Value.AsValues().Select(v => (string) v.RawValue!).ToArray());
    }
}
