using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.PhysicsEngine;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Component;

public class UPrimitiveComponent : USceneComponent
{
    public bool CastShadow { get; protected set; }
    public bool bCastDynamicShadow { get; protected set; } = true;
    public bool bCastStaticShadow { get; private set; } = true;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        CastShadow = GetOrDefault(nameof(CastShadow), CastShadow);
        bCastDynamicShadow = GetOrDefault(nameof(bCastDynamicShadow), bCastDynamicShadow);
        bCastStaticShadow = GetOrDefault(nameof(bCastStaticShadow), bCastStaticShadow);

        if (Ar.Game == GAME_WorldofJadeDynasty) Ar.Position += 16;
        if (Ar.Ver >= EUnrealEngineObjectUE3Version.AddedComponentGuid && Ar.Ver < EUnrealEngineObjectUE3Version.REMOVED_COMPONENT_GUID)
        {
            Ar.Position += 16; // Guid
        }
    }

    public virtual UBodySetup? GetBodySetup() => null;
}
