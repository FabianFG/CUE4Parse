using System.ComponentModel;

namespace CUE4Parse.UE4.Objects.PhysicsEngine;

public enum EBodyCollisionResponse : byte
{
    [Description("Enabled")]
    BodyCollision_Enabled,
    [Description("Disabled")]
    BodyCollision_Disabled,
    // [Description("Custom")]
    // BodyCollision_Custom
}
