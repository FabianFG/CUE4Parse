using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Component;

public enum ECameraProjectionMode : byte
{
    Perspective,
    Orthographic
}

public class UCameraComponent : USceneComponent
{
    public float FieldOfView { get; private set; } = 90.0f;
    public float OrthoWidth { get; private set; } = 1536.0f;
    public float OrthoNearClipPlane { get; private set; } = -768.0f;
    public float OrthoFarClipPlane { get; private set; } = 2097152.0f;
    public float AspectRatio { get; private set; } = 1.777778f;
    public bool bConstrainAspectRatio { get; private set; }
    public bool bAutoCalculateOrthoPlanes { get; private set; } = true;
    public ECameraProjectionMode ProjectionMode { get; private set; } = ECameraProjectionMode.Perspective;
    public float PostProcessBlendWeight { get; private set; } = 1.0f;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        if (FFortniteMainBranchObjectVersion.Get(Ar) < FFortniteMainBranchObjectVersion.Type.OrthographicCameraDefaultSettings)
        {
            OrthoWidth = 512.0f;
            OrthoNearClipPlane = 0.0f;
            // OrthoFarClipPlane = 2097152.0f;
        }

        if (FUE5ReleaseStreamObjectVersion.Get(Ar) < FUE5ReleaseStreamObjectVersion.Type.OrthographicAutoNearFarPlane)
        {
            bAutoCalculateOrthoPlanes = false;
        }

        base.Deserialize(Ar, validPos);

        FieldOfView = GetOrDefault(nameof(FieldOfView), FieldOfView);
        OrthoWidth = GetOrDefault(nameof(OrthoWidth), OrthoWidth);
        OrthoNearClipPlane = GetOrDefault(nameof(OrthoNearClipPlane), OrthoNearClipPlane);
        OrthoFarClipPlane = GetOrDefault(nameof(OrthoFarClipPlane), OrthoFarClipPlane);
        AspectRatio = GetOrDefault(nameof(AspectRatio), AspectRatio);
        bConstrainAspectRatio = GetOrDefault(nameof(bConstrainAspectRatio), bConstrainAspectRatio);
        bAutoCalculateOrthoPlanes = GetOrDefault(nameof(bAutoCalculateOrthoPlanes), bAutoCalculateOrthoPlanes);
        ProjectionMode = GetOrDefault(nameof(ProjectionMode), ProjectionMode);
        PostProcessBlendWeight = GetOrDefault(nameof(PostProcessBlendWeight), PostProcessBlendWeight);
    }
}
