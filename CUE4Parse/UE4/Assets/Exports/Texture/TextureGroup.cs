namespace CUE4Parse.UE4.Assets.Exports.Texture;

public enum TextureGroup
{
    TEXTUREGROUP_World,
	TEXTUREGROUP_WorldNormalMap,
	TEXTUREGROUP_WorldSpecular,
	TEXTUREGROUP_Character,
	TEXTUREGROUP_CharacterNormalMap,
	TEXTUREGROUP_CharacterSpecular,
	TEXTUREGROUP_Weapon,
	TEXTUREGROUP_WeaponNormalMap,
	TEXTUREGROUP_WeaponSpecular,
	TEXTUREGROUP_Vehicle,
	TEXTUREGROUP_VehicleNormalMap,
	TEXTUREGROUP_VehicleSpecular,
	TEXTUREGROUP_Cinematic,
	TEXTUREGROUP_Effects,
	TEXTUREGROUP_EffectsNotFiltered,
	TEXTUREGROUP_Skybox,
	TEXTUREGROUP_UI,
	TEXTUREGROUP_Lightmap,
	TEXTUREGROUP_RenderTarget,
	TEXTUREGROUP_MobileFlattened,
	/** Obsolete - kept for backwards compatibility. */
	TEXTUREGROUP_ProcBuilding_Face,
	/** Obsolete - kept for backwards compatibility. */
	TEXTUREGROUP_ProcBuilding_LightMap,
	TEXTUREGROUP_Shadowmap,
	/** No compression, no mips. */
	TEXTUREGROUP_ColorLookupTable,
	TEXTUREGROUP_Terrain_Heightmap,
	TEXTUREGROUP_Terrain_Weightmap,
	/** Using this TextureGroup triggers special mip map generation code only useful for the BokehDOF post process. */
	TEXTUREGROUP_Bokeh,
	/** No compression, created on import of a .IES file. */
	TEXTUREGROUP_IESLightProfile,
	/** Non-filtered, useful for 2D rendering. */
	TEXTUREGROUP_Pixels2D,
	/** Hierarchical LOD generated textures*/
	TEXTUREGROUP_HierarchicalLOD,
	/** Impostor Color Textures*/
	TEXTUREGROUP_Impostor,
	/** Impostor Normal and Depth, use default compression*/
	TEXTUREGROUP_ImpostorNormalDepth,
	/** 8 bit data stored in textures */
	TEXTUREGROUP_8BitData,
	/** 16 bit data stored in textures */
	TEXTUREGROUP_16BitData,
	TEXTUREGROUP_MAX = 66,
}