using System.ComponentModel;

namespace CUE4Parse.UE4.Objects.PhysicsEngine;

public enum ECollisionEnabled : byte
{
    /** Will not create any representation in the physics engine. Cannot be used for spatial queries (raycasts, sweeps, overlaps) or simulation (rigid body, constraints). Best performance possible (especially for moving objects) */
    [Description("No Collision")]
    NoCollision,
    /** Only used for spatial queries (raycasts, sweeps, and overlaps). Cannot be used for simulation (rigid body, constraints). Useful for character movement and things that do not need physical simulation. Performance gains by keeping data out of simulation tree. */
    [Description("Query Only (No Physics Collision)")]
    QueryOnly,
    /** Only used only for physics simulation (rigid body, constraints). Cannot be used for spatial queries (raycasts, sweeps, overlaps). Useful for jiggly bits on characters that do not need per bone detection. Performance gains by keeping data out of query tree */
    [Description("Physics Only (No Query Collision)")]
    PhysicsOnly,
    /** Can be used for both spatial queries (raycasts, sweeps, overlaps) and simulation (rigid body, constraints). */
    [Description("Collision Enabled (Query and Physics)")]
    QueryAndPhysics,
    /** Only used for probing the physics simulation (rigid body, constraints). Cannot be used for spatial queries (raycasts,
    sweeps, overlaps). Useful for when you want to detect potential physics interactions and pass contact data to hit callbacks
    or contact modification, but don't want to physically react to these contacts. */
    [Description("Probe Only (Contact Data, No Query or Physics Collision)")]
    ProbeOnly,
    /** Can be used for both spatial queries (raycasts, sweeps, overlaps) and probing the physics simulation (rigid body,
    constraints). Will not allow for actual physics interaction, but will generate contact data, trigger hit callbacks, and
    contacts will appear in contact modification. */
    [Description("Query and Probe (Query Collision and Contact Data, No Physics Collision)")]
    QueryAndProbe
}
