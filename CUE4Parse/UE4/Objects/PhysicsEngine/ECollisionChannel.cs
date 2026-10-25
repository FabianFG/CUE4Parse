using System.ComponentModel;

namespace CUE4Parse.UE4.Objects.PhysicsEngine;

public enum ECollisionChannel : byte
{
    [Description("WorldStatic")]
    ECC_WorldStatic,
    [Description("WorldDynamic")]
	ECC_WorldDynamic,
    [Description("Pawn")]
	ECC_Pawn,
    [Description("Visibility")]
	ECC_Visibility,
    [Description("Camera")]
	ECC_Camera,
    [Description("PhysicsBody")]
	ECC_PhysicsBody,
    [Description("Vehicle")]
	ECC_Vehicle,
    [Description("Destructible")]
	ECC_Destructible,

	/** Reserved for gizmo collision */
	ECC_EngineTraceChannel1,

	ECC_EngineTraceChannel2,
	ECC_EngineTraceChannel3,
	ECC_EngineTraceChannel4,
	ECC_EngineTraceChannel5,
	ECC_EngineTraceChannel6,

	ECC_GameTraceChannel1,
	ECC_GameTraceChannel2,
	ECC_GameTraceChannel3,
	ECC_GameTraceChannel4,
	ECC_GameTraceChannel5,
	ECC_GameTraceChannel6,
	ECC_GameTraceChannel7,
	ECC_GameTraceChannel8,
	ECC_GameTraceChannel9,
	ECC_GameTraceChannel10,
	ECC_GameTraceChannel11,
	ECC_GameTraceChannel12,
	ECC_GameTraceChannel13,
	ECC_GameTraceChannel14,
	ECC_GameTraceChannel15,
	ECC_GameTraceChannel16,
	ECC_GameTraceChannel17,
	ECC_GameTraceChannel18,
	ECC_GameTraceChannel19,
	ECC_GameTraceChannel20,
	ECC_GameTraceChannel21,
	ECC_GameTraceChannel22,
	ECC_GameTraceChannel23,
	ECC_GameTraceChannel24,
	ECC_GameTraceChannel25,
	ECC_GameTraceChannel26,
	ECC_GameTraceChannel27,
	ECC_GameTraceChannel28,
	ECC_GameTraceChannel29,
	ECC_GameTraceChannel30,
	ECC_GameTraceChannel31,
	ECC_GameTraceChannel32,
	ECC_GameTraceChannel33,
	ECC_GameTraceChannel34,
	ECC_GameTraceChannel35,
	ECC_GameTraceChannel36,
	ECC_GameTraceChannel37,
	ECC_GameTraceChannel38,
	ECC_GameTraceChannel39,
	ECC_GameTraceChannel40,
	ECC_GameTraceChannel41,
	ECC_GameTraceChannel42,
	ECC_GameTraceChannel43,
	ECC_GameTraceChannel44,
	ECC_GameTraceChannel45,
	ECC_GameTraceChannel46,
	ECC_GameTraceChannel47,
	ECC_GameTraceChannel48,
	ECC_GameTraceChannel49,
	ECC_GameTraceChannel50,

	/** Add new serializeable channels above here (i.e. entries that exist in FCollisionResponseContainer) */
	/** Add only nonserialized/transient flags below */

	// NOTE!!!! THESE ARE BEING DEPRECATED BUT STILL THERE FOR BLUEPRINT. PLEASE DO NOT USE THEM IN CODE

	ECC_OverlapAll_Deprecated,
	ECC_MAX,
}
