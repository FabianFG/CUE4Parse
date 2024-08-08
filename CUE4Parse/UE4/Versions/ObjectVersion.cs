using System;
using System.Runtime.CompilerServices;

namespace CUE4Parse.UE4.Versions
{
    public enum EUnrealEngineObjectUE5Version : uint
    {
        // Note that currently the oldest loadable package version is EUnrealEngineObjectUE4Version.OLDEST_LOADABLE_PACKAGE
        // this can be enabled should we ever deprecate UE4 versions entirely
        //OLDEST_LOADABLE_PACKAGE = ???,

        // The original UE5 version, at the time this was added the UE4 version was 522, so UE5 will start from 1000 to show a clear difference
        INITIAL_VERSION = 1000,

        // Support stripping names that are not referenced from export data
        NAMES_REFERENCED_FROM_EXPORT_DATA,

        // Added a payload table of contents to the package summary
        PAYLOAD_TOC,

        // Added data to identify references from and to optional package
        OPTIONAL_RESOURCES,

        // Large world coordinates converts a number of core types to double components by default.
        LARGE_WORLD_COORDINATES,

        // Remove package GUID from FObjectExport
        REMOVE_OBJECT_EXPORT_PACKAGE_GUID,

        // Add IsInherited to the FObjectExport entry
        TRACK_OBJECT_EXPORT_IS_INHERITED,

        // Replace FName asset path in FSoftObjectPath with (package name, asset name) pair FTopLevelAssetPath
        FSOFTOBJECTPATH_REMOVE_ASSET_PATH_FNAMES,

        // Add a soft object path list to the package summary for fast remap
        ADD_SOFTOBJECTPATH_LIST,

        // Added bulk/data resource table
        DATA_RESOURCES,

        // Added script property serialization offset to export table entries for saved, versioned packages
        SCRIPT_SERIALIZATION_OFFSET,

        // Adding property tag extension,
        // Support for overridable serialization on UObject,
        // Support for overridable logic in containers
        PROPERTY_TAG_EXTENSION_AND_OVERRIDABLE_SERIALIZATION,

        // Added property tag complete type name and serialization type
        PROPERTY_TAG_COMPLETE_TYPE_NAME,

        // -----<new versions can be added before this line>-------------------------------------------------
        // - this needs to be the last line (see note below)
        AUTOMATIC_VERSION_PLUS_ONE,
        AUTOMATIC_VERSION = AUTOMATIC_VERSION_PLUS_ONE - 1
    }

    public enum EUnrealEngineObjectUE4Version
    {
        DETERMINE_BY_GAME = 0,

        // Pre-release UE4 file versions
        ASSET_REGISTRY_TAGS = 112,
        TEXTURE_DERIVED_DATA2 = 124,
        ADD_COOKED_TO_TEXTURE2D = 125,
        REMOVED_STRIP_DATA = 130,
        REMOVE_EXTRA_SKELMESH_VERTEX_INFLUENCES = 134,
        TEXTURE_SOURCE_ART_REFACTOR = 143,
        ADD_SKELMESH_MESHTOIMPORTVERTEXMAP = 152,
        REMOVE_ARCHETYPE_INDEX_FROM_LINKER_TABLES = 163,
        REMOVE_NET_INDEX = 196,
        BULKDATA_AT_LARGE_OFFSETS = 198,
        SUMMARY_HAS_BULKDATA_OFFSET = 212,

        OLDEST_LOADABLE_PACKAGE = 214,

        // Removed restriction on blueprint-exposed variables from being read-only
        BLUEPRINT_VARS_NOT_READ_ONLY,
        // Added manually serialized element to UStaticMesh (precalculated nav collision)
        STATIC_MESH_STORE_NAV_COLLISION,
        // Changed property name for atmospheric fog
        ATMOSPHERIC_FOG_DECAY_NAME_CHANGE,
        // Change many properties/functions from Translation to Location
        SCENECOMP_TRANSLATION_TO_LOCATION,
        // Material attributes reordering
        MATERIAL_ATTRIBUTES_REORDERING,
        // Collision Profile setting has been added, and all components that exists has to be properly upgraded
        COLLISION_PROFILE_SETTING,
        // Making the blueprint's skeleton class transient
        BLUEPRINT_SKEL_TEMPORARY_TRANSIENT,
        // Making the blueprint's skeleton class serialized again
        BLUEPRINT_SKEL_SERIALIZED_AGAIN,
        // Blueprint now controls replication settings again
        BLUEPRINT_SETS_REPLICATION,
        // Added level info used by World browser
        WORLD_LEVEL_INFO,
        // Changed capsule height to capsule half-height (afterwards)
        AFTER_CAPSULE_HALF_HEIGHT_CHANGE,
        // Added Namepace, GUID (Key) and Flags to FText
        ADDED_NAMESPACE_AND_KEY_DATA_TO_FTEXT,
        // Attenuation shapes
        ATTENUATION_SHAPES,
        // Use IES texture multiplier even when IES brightness is not being used
        LIGHTCOMPONENT_USE_IES_TEXTURE_MULTIPLIER_ON_NON_IES_BRIGHTNESS,
        // Removed InputComponent as a blueprint addable component
        REMOVE_INPUT_COMPONENTS_FROM_BLUEPRINTS,
        // Use an FMemberReference struct in UK2Node_Variable
        VARK2NODE_USE_MEMBERREFSTRUCT,
        // Refactored material expression inputs for UMaterialExpressionSceneColor and UMaterialExpressionSceneDepth
        REFACTOR_MATERIAL_EXPRESSION_SCENECOLOR_AND_SCENEDEPTH_INPUTS,
        // Spline meshes changed from Z forwards to configurable
        SPLINE_MESH_ORIENTATION,
        // Added ReverbEffect asset type
        REVERB_EFFECT_ASSET_TYPE,
        // changed max texcoords from 4 to 8
        MAX_TEXCOORD_INCREASED,
        // static meshes changed to support SpeedTrees
        SPEEDTREE_STATICMESH,
        // Landscape component reference between landscape component and collision component
        LANDSCAPE_COMPONENT_LAZY_REFERENCES,
        // Refactored UK2Node_CallFunction to use FMemberReference
        SWITCH_CALL_NODE_TO_USE_MEMBER_REFERENCE,
        // Added fixup step to remove skeleton class references from blueprint objects
        ADDED_SKELETON_ARCHIVER_REMOVAL,
        // See above, take 2.
        ADDED_SKELETON_ARCHIVER_REMOVAL_SECOND_TIME,
        // Making the skeleton class on blueprints transient
        BLUEPRINT_SKEL_CLASS_TRANSIENT_AGAIN,
        // UClass knows if it's been cooked
        ADD_COOKED_TO_UCLASS,
        // Deprecated static mesh thumbnail properties were removed
        DEPRECATED_STATIC_MESH_THUMBNAIL_PROPERTIES_REMOVED,
        // Added collections in material shader map ids
        COLLECTIONS_IN_SHADERMAPID,
        // Renamed some Movement Component properties, added PawnMovementComponent
        REFACTOR_MOVEMENT_COMPONENT_HIERARCHY,
        // Swap UMaterialExpressionTerrainLayerSwitch::LayerUsed/LayerNotUsed the correct way round
        FIX_TERRAIN_LAYER_SWITCH_ORDER,
        // Remove URB_ConstraintSetup
        ALL_PROPS_TO_CONSTRAINTINSTANCE,
        // Low quality directional lightmaps
        LOW_QUALITY_DIRECTIONAL_LIGHTMAPS,
        // Added NoiseEmitterComponent and removed related Pawn properties.
        ADDED_NOISE_EMITTER_COMPONENT,
        // Add text component vertical alignment
        ADD_TEXT_COMPONENT_VERTICAL_ALIGNMENT,
        // Added AssetImportData for FBX asset types, deprecating SourceFilePath and SourceFileTimestamp
        ADDED_FBX_ASSET_IMPORT_DATA,
        // Remove LevelBodySetup from ULevel
        REMOVE_LEVELBODYSETUP,
        // Refactor character crouching
        REFACTOR_CHARACTER_CROUCH,
        // Trimmed down material shader debug information.
        SMALLER_DEBUG_MATERIALSHADER_UNIFORM_EXPRESSIONS,
        // APEX Clothing
        APEX_CLOTH,
        // Change Collision Channel to save only modified ones than all of them
        // @note!!! once we pass this CL, we can rename FCollisionResponseContainer enum values
        // we should rename to match ECollisionChannel
        SAVE_COLLISIONRESPONSE_PER_CHANNEL,
        // Added Landscape Spline editor meshes
        ADDED_LANDSCAPE_SPLINE_EDITOR_MESH,
        // Fixup input expressions for reading from refraction material attributes.
        CHANGED_MATERIAL_REFACTION_TYPE,
        // Refactor projectile movement, along with some other movement component work.
        REFACTOR_PROJECTILE_MOVEMENT,
        // Remove PhysicalMaterialProperty and replace with user defined enum
        REMOVE_PHYSICALMATERIALPROPERTY,
        // Removed all compile outputs from FMaterial
        PURGED_FMATERIAL_COMPILE_OUTPUTS,
        // Ability to save cooked PhysX meshes to Landscape
        ADD_COOKED_TO_LANDSCAPE,
        // Change how input component consumption works
        CONSUME_INPUT_PER_BIND,
        // Added new Graph based SoundClass Editor
        SOUND_CLASS_GRAPH_EDITOR,
        // Fixed terrain layer node guids which was causing artifacts
        FIXUP_TERRAIN_LAYER_NODES,
        // Added clamp min/max swap check to catch older materials
        RETROFIT_CLAMP_EXPRESSIONS_SWAP,
        // Remove static/movable/stationary light classes
        REMOVE_LIGHT_MOBILITY_CLASSES,
        // Refactor the way physics blending works to allow partial blending
        REFACTOR_PHYSICS_BLENDING,
        // WorldLevelInfo: Added reference to parent level and streaming distance
        WORLD_LEVEL_INFO_UPDATED,
        // Fixed cooking of skeletal/static meshes due to bad serialization logic
        STATIC_SKELETAL_MESH_SERIALIZATION_FIX,
        // Removal of InterpActor and PhysicsActor
        REMOVE_STATICMESH_MOBILITY_CLASSES,
        // Refactor physics transforms
        REFACTOR_PHYSICS_TRANSFORMS,
        // Remove zero triangle sections from static meshes and compact material indices.
        REMOVE_ZERO_TRIANGLE_SECTIONS,
        // Add param for deceleration in character movement instead of using acceleration.
        CHARACTER_MOVEMENT_DECELERATION,
        // Made ACameraActor use a UCameraComponent for parameter storage, etc...
        CAMERA_ACTOR_USING_CAMERA_COMPONENT,
        // Deprecated some pitch/roll properties in CharacterMovementComponent
        CHARACTER_MOVEMENT_DEPRECATE_PITCH_ROLL,
        // Rebuild texture streaming data on load for uncooked builds
        REBUILD_TEXTURE_STREAMING_DATA_ON_LOAD,
        // Add support for 32 bit index buffers for static meshes.
        SUPPORT_32BIT_STATIC_MESH_INDICES,
        // Added streaming install ChunkID to AssetData and UPackage
        ADDED_CHUNKID_TO_ASSETDATA_AND_UPACKAGE,
        // Add flag to control whether Character blueprints receive default movement bindings.
        CHARACTER_DEFAULT_MOVEMENT_BINDINGS,
        // APEX Clothing LOD Info
        APEX_CLOTH_LOD,
        // Added atmospheric fog texture data to be general
        ATMOSPHERIC_FOG_CACHE_DATA,
        // Arrays serialize their inner's tags
        ARRAY_PROPERTY_INNER_TAGS,
        // Skeletal mesh index data is kept in memory in game to support mesh merging.
        KEEP_SKEL_MESH_INDEX_DATA,
        // Added compatibility for the body instance collision change
        BODYSETUP_COLLISION_CONVERSION,
        // Reflection capture cooking
        REFLECTION_CAPTURE_COOKING,
        // Removal of DynamicTriggerVolume, DynamicBlockingVolume, DynamicPhysicsVolume
        REMOVE_DYNAMIC_VOLUME_CLASSES,
        // Store an additional flag in the BodySetup to indicate whether there is any cooked data to load
        STORE_HASCOOKEDDATA_FOR_BODYSETUP,
        // Changed name of RefractionBias to RefractionDepthBias.
        REFRACTION_BIAS_TO_REFRACTION_DEPTH_BIAS,
        // Removal of SkeletalPhysicsActor
        REMOVE_SKELETALPHYSICSACTOR,
        // PlayerController rotation input refactor
        PC_ROTATION_INPUT_REFACTOR,
        // Landscape Platform Data cooking
        LANDSCAPE_PLATFORMDATA_COOKING,
        // Added call for linking classes in CreateExport to ensure memory is initialized properly
        CREATEEXPORTS_CLASS_LINKING_FOR_BLUEPRINTS,
        // Remove native component nodes from the blueprint SimpleConstructionScript
        REMOVE_NATIVE_COMPONENTS_FROM_BLUEPRINT_SCS,
        // Removal of Single Node Instance
        REMOVE_SINGLENODEINSTANCE,
        // Character movement braking changes
        CHARACTER_BRAKING_REFACTOR,
        // Supported low quality lightmaps in volume samples
        VOLUME_SAMPLE_LOW_QUALITY_SUPPORT,
        // Split bEnableTouchEvents out from bEnableClickEvents
        SPLIT_TOUCH_AND_CLICK_ENABLES,
        // Health/Death refactor
        HEALTH_DEATH_REFACTOR,
        // Moving USoundNodeEnveloper from UDistributionFloatConstantCurve to FRichCurve
        SOUND_NODE_ENVELOPER_CURVE_CHANGE,
        // Moved SourceRadius to UPointLightComponent
        POINT_LIGHT_SOURCE_RADIUS,
        // Scene capture actors based on camera actors.
        SCENE_CAPTURE_CAMERA_CHANGE,
        // Moving SkeletalMesh shadow casting flag from LoD details to material
        MOVE_SKELETALMESH_SHADOWCASTING,
        // Changing bytecode operators for creating arrays
        CHANGE_SETARRAY_BYTECODE,
        // Material Instances overriding base material properties.
        MATERIAL_INSTANCE_BASE_PROPERTY_OVERRIDES,
        // Combined top/bottom lightmap textures
        COMBINED_LIGHTMAP_TEXTURES,
        // Forced material lightmass guids to be regenerated
        BUMPED_MATERIAL_EXPORT_GUIDS,
        // Allow overriding of parent class input bindings
        BLUEPRINT_INPUT_BINDING_OVERRIDES,
        // Fix up convex invalid transform
        FIXUP_BODYSETUP_INVALID_CONVEX_TRANSFORM,
        // Fix up scale of physics stiffness and damping value
        FIXUP_STIFFNESS_AND_DAMPING_SCALE,
        // Convert USkeleton and FBoneContrainer to using FReferenceSkeleton.
        REFERENCE_SKELETON_REFACTOR,
        // Adding references to variable, function, and macro nodes to be able to update to renamed values
        K2NODE_REFERENCEGUIDS,
        // Fix up the 0th bone's parent bone index.
        FIXUP_ROOTBONE_PARENT,
        //Allow setting of TextRenderComponents size in world space.
        TEXT_RENDER_COMPONENTS_WORLD_SPACE_SIZING,
        // Material Instances overriding base material properties #2.
        MATERIAL_INSTANCE_BASE_PROPERTY_OVERRIDES_PHASE_2,
        // CLASS_Placeable becomes CLASS_NotPlaceable
        CLASS_NOTPLACEABLE_ADDED,
        // Added LOD info list to a world tile description
        WORLD_LEVEL_INFO_LOD_LIST,
        // CharacterMovement variable naming refactor
        CHARACTER_MOVEMENT_VARIABLE_RENAMING_1,
        // FName properties containing sound names converted to FSlateSound properties
        FSLATESOUND_CONVERSION,
        // Added ZOrder to a world tile description
        WORLD_LEVEL_INFO_ZORDER,
        // Added flagging of localization gather requirement to packages
        PACKAGE_REQUIRES_LOCALIZATION_GATHER_FLAGGING,
        // Preventing Blueprint Actor variables from having default values
        BP_ACTOR_VARIABLE_DEFAULT_PREVENTING,
        // Preventing Blueprint Actor variables from having default values
        TEST_ANIMCOMP_CHANGE,
        // Class as primary asset, name convention changed
        EDITORONLY_BLUEPRINTS,
        // Custom serialization for FEdGraphPinType
        EDGRAPHPINTYPE_SERIALIZATION,
        // Stop generating 'mirrored' cooked mesh for Brush and Model components
        NO_MIRROR_BRUSH_MODEL_COLLISION,
        // Changed ChunkID to be an array of IDs.
        CHANGED_CHUNKID_TO_BE_AN_ARRAY_OF_CHUNKIDS,
        // Worlds have been renamed from "TheWorld" to be named after the package containing them
        WORLD_NAMED_AFTER_PACKAGE,
        // Added sky light component
        SKY_LIGHT_COMPONENT,
        // Added Enable distance streaming flag to FWorldTileLayer
        WORLD_LAYER_ENABLE_DISTANCE_STREAMING,
        // Remove visibility/zone information from UModel
        REMOVE_ZONES_FROM_MODEL,
        // Fix base pose serialization
        FIX_ANIMATIONBASEPOSE_SERIALIZATION,
        // Support for up to 8 skinning influences per vertex on skeletal meshes (on non-gpu vertices)
        SUPPORT_8_BONE_INFLUENCES_SKELETAL_MESHES,
        // Add explicit bOverrideGravity to world settings
        ADD_OVERRIDE_GRAVITY_FLAG,
        // Support for up to 8 skinning influences per vertex on skeletal meshes (on gpu vertices)
        SUPPORT_GPUSKINNING_8_BONE_INFLUENCES,
        // Supporting nonuniform scale animation
        ANIM_SUPPORT_NONUNIFORM_SCALE_ANIMATION,
        // Engine version is stored as a FEngineVersion object rather than changelist number
        ENGINE_VERSION_OBJECT,
        // World assets now have RF_Public
        PUBLIC_WORLDS,
        // Skeleton Guid
        SKELETON_GUID_SERIALIZATION,
        // Character movement WalkableFloor refactor
        CHARACTER_MOVEMENT_WALKABLE_FLOOR_REFACTOR,
        // Lights default to inverse squared
        INVERSE_SQUARED_LIGHTS_DEFAULT,
        // Disabled SCRIPT_LIMIT_BYTECODE_TO_64KB
        DISABLED_SCRIPT_LIMIT_BYTECODE,
        // Made remote role private, exposed bReplicates
        PRIVATE_REMOTE_ROLE,
        // Fix up old foliage components to have static mobility (superseded by FOLIAGE_MOVABLE_MOBILITY)
        FOLIAGE_STATIC_MOBILITY,
        // Change BuildScale from a float to a vector
        BUILD_SCALE_VECTOR,
        // After implementing foliage collision, need to disable collision on old foliage instances
        FOLIAGE_COLLISION,
        // Added sky bent normal to indirect lighting cache
        SKY_BENT_NORMAL,
        // Added cooking for landscape collision data
        LANDSCAPE_COLLISION_DATA_COOKING,
        // Convert CPU tangent Z delta to vector from PackedNormal since we don't get any benefit other than memory
        // we still convert all to FVector in CPU time whenever any calculation
        MORPHTARGET_CPU_TANGENTZDELTA_FORMATCHANGE,
        // Soft constraint limits will implicitly use the mass of the bodies
        SOFT_CONSTRAINTS_USE_MASS,
        // Reflection capture data saved in packages
        REFLECTION_DATA_IN_PACKAGES,
        // Fix up old foliage components to have movable mobility (superseded by FOLIAGE_STATIC_LIGHTING_SUPPORT)
        FOLIAGE_MOVABLE_MOBILITY,
        // Undo BreakMaterialAttributes changes as it broke old content
        UNDO_BREAK_MATERIALATTRIBUTES_CHANGE,
        // Now Default custom profile name isn't NONE anymore due to copy/paste not working properly with it
        ADD_CUSTOMPROFILENAME_CHANGE,
        // Permanently flip and scale material expression coordinates
        FLIP_MATERIAL_COORDS,
        // PinSubCategoryMemberReference added to FEdGraphPinType
        MEMBERREFERENCE_IN_PINTYPE,
        // Vehicles use Nm for Torque instead of cm and RPM instead of rad/s
        VEHICLES_UNIT_CHANGE,
        // removes NANs from all animations when loaded
        // now importing should detect NaNs, so we should not have NaNs in source data
        ANIMATION_REMOVE_NANS,
        // Change skeleton preview attached assets property type
        SKELETON_ASSET_PROPERTY_TYPE_CHANGE,
        // Fix some blueprint variables that have the CPF_DisableEditOnTemplate flag set
        // when they shouldn't
        FIX_BLUEPRINT_VARIABLE_FLAGS,
        // Vehicles use Nm for Torque instead of cm and RPM instead of rad/s part two (missed conversion for some variables
        VEHICLES_UNIT_CHANGE2,
        // Changed order of interface class serialization
        UCLASS_SERIALIZE_INTERFACES_AFTER_LINKING,
        // Change from LOD distances to display factors
        STATIC_MESH_SCREEN_SIZE_LODS,
        // Requires test of material coords to ensure they're saved correctly
        FIX_MATERIAL_COORDS,
        // Changed SpeedTree wind presets to v7
        SPEEDTREE_WIND_V7,
        // NeedsLoadForEditorGame added
        LOAD_FOR_EDITOR_GAME,
        // Manual serialization of FRichCurveKey to save space
        SERIALIZE_RICH_CURVE_KEY,
        // Change the outer of ULandscapeMaterialInstanceConstants and Landscape-related textures to the level in which they reside
        MOVE_LANDSCAPE_MICS_AND_TEXTURES_WITHIN_LEVEL,
        // FTexts have creation history data, removed Key, Namespaces, and SourceString
        FTEXT_HISTORY,
        // Shift comments to the left to contain expressions properly
        FIX_MATERIAL_COMMENTS,
        // Bone names stored as FName means that we can't guarantee the correct case on export, now we store a separate string for export purposes only
        STORE_BONE_EXPORT_NAMES,
        // changed mesh emitter initial orientation to distribution
        MESH_EMITTER_INITIAL_ORIENTATION_DISTRIBUTION,
        // Foliage on blueprints causes crashes
        DISALLOW_FOLIAGE_ON_BLUEPRINTS,
        // change motors to use revolutions per second instead of rads/second
        FIXUP_MOTOR_UNITS,
        // deprecated MovementComponent functions including "ModifiedMaxSpeed" et al
        DEPRECATED_MOVEMENTCOMPONENT_MODIFIED_SPEEDS,
        // rename CanBeCharacterBase
        RENAME_CANBECHARACTERBASE,
        // Change GameplayTagContainers to have FGameplayTags instead of FNames; Required to fix-up native serialization
        GAMEPLAY_TAG_CONTAINER_TAG_TYPE_CHANGE,
        // Change from UInstancedFoliageSettings to UFoliageType, and change the api from being keyed on UStaticMesh* to UFoliageType*
        FOLIAGE_SETTINGS_TYPE,
        // Lights serialize static shadow depth maps
        STATIC_SHADOW_DEPTH_MAPS,
        // Add RF_Transactional to data assets, fixing undo problems when editing them
        ADD_TRANSACTIONAL_TO_DATA_ASSETS,
        // Change LB_AlphaBlend to LB_WeightBlend in ELandscapeLayerBlendType
        ADD_LB_WEIGHTBLEND,
        // Add root component to an foliage actor, all foliage cluster components will be attached to a root
        ADD_ROOTCOMPONENT_TO_FOLIAGEACTOR,
        // FMaterialInstanceBasePropertyOverrides didn't use proper UObject serialize
        FIX_MATERIAL_PROPERTY_OVERRIDE_SERIALIZE,
        // Addition of linear color sampler. color sample type is changed to linear sampler if source texture !sRGB
        ADD_LINEAR_COLOR_SAMPLER,
        // Added StringAssetReferencesMap to support renames of FStringAssetReference properties.
        ADD_STRING_ASSET_REFERENCES_MAP,
        // Apply scale from SCS RootComponent details in the Blueprint Editor to new actor instances at construction time
        BLUEPRINT_USE_SCS_ROOTCOMPONENT_SCALE,
        // Changed level streaming to have a linear color since the visualization doesn't gamma correct.
        LEVEL_STREAMING_DRAW_COLOR_TYPE_CHANGE,
        // Cleared end triggers from non-state anim notifies
        CLEAR_NOTIFY_TRIGGERS,
        // Convert old curve names stored in anim assets into skeleton smartnames
        SKELETON_ADD_SMARTNAMES,
        // Added the currency code field to FTextHistory_AsCurrency
        ADDED_CURRENCY_CODE_TO_FTEXT,
        // Added support for C++11 enum classes
        ENUM_CLASS_SUPPORT,
        // Fixup widget animation class
        FIXUP_WIDGET_ANIMATION_CLASS,
        // USoundWave objects now contain details about compression scheme used.
        SOUND_COMPRESSION_TYPE_ADDED,
        // Bodies will automatically weld when attached
        AUTO_WELDING,
        // Rename UCharacterMovementComponent::bCrouchMovesCharacterDown
        RENAME_CROUCHMOVESCHARACTERDOWN,
        // Lightmap parameters in FMeshBuildSettings
        LIGHTMAP_MESH_BUILD_SETTINGS,
        // Rename SM3 to ES3_1 and updates featurelevel material node selector
        RENAME_SM3_TO_ES3_1,
        // Deprecated separate style assets for use in UMG
        DEPRECATE_UMG_STYLE_ASSETS,
        // Duplicating Blueprints will regenerate NodeGuids after this version
        POST_DUPLICATE_NODE_GUID,
        // Rename USpringArmComponent::bUseControllerViewRotation to bUsePawnViewRotation,
        // Rename UCameraComponent::bUseControllerViewRotation to bUsePawnViewRotation (and change the default value)
        RENAME_CAMERA_COMPONENT_VIEW_ROTATION,
        // Changed FName to be case preserving
        CASE_PRESERVING_FNAME,
        // Rename USpringArmComponent::bUsePawnViewRotation to bUsePawnControlRotation
        // Rename UCameraComponent::bUsePawnViewRotation to bUsePawnControlRotation
        RENAME_CAMERA_COMPONENT_CONTROL_ROTATION,
        // Fix bad refraction material attribute masks
        FIX_REFRACTION_INPUT_MASKING,
        // A global spawn rate for emitters.
        GLOBAL_EMITTER_SPAWN_RATE_SCALE,
        // Cleanup destructible mesh settings
        CLEAN_DESTRUCTIBLE_SETTINGS,
        // CharacterMovementComponent refactor of AdjustUpperHemisphereImpact and deprecation of some associated vars.
        CHARACTER_MOVEMENT_UPPER_IMPACT_BEHAVIOR,
        // Changed Blueprint math equality functions for vectors and rotators to operate as a "nearly" equals rather than "exact"
        BP_MATH_VECTOR_EQUALITY_USES_EPSILON,
        // Static lighting support was re-added to foliage, and mobility was returned to static
        FOLIAGE_STATIC_LIGHTING_SUPPORT,
        // Added composite fonts to Slate font info
        SLATE_COMPOSITE_FONTS,
        // Remove UDEPRECATED_SaveGameSummary, required for UWorld::Serialize
        REMOVE_SAVEGAMESUMMARY,

        //Remove bodyseutp serialization from skeletal mesh component
        REMOVE_SKELETALMESH_COMPONENT_BODYSETUP_SERIALIZATION,
        // Made Slate font data use bulk data to store the embedded font data
        SLATE_BULK_FONT_DATA,
        // Add new friction behavior in ProjectileMovementComponent.
        ADD_PROJECTILE_FRICTION_BEHAVIOR,
        // Add axis settings enum to MovementComponent.
        MOVEMENTCOMPONENT_AXIS_SETTINGS,
        // Switch to new interactive comments, requires boundry conversion to preserve previous states
        GRAPH_INTERACTIVE_COMMENTBUBBLES,
        // Landscape serializes physical materials for collision objects
        LANDSCAPE_SERIALIZE_PHYSICS_MATERIALS,
        // Rename Visiblity on widgets to Visibility
        RENAME_WIDGET_VISIBILITY,
        // add track curves for animation
        ANIMATION_ADD_TRACKCURVES,
        // Removed BranchingPoints from AnimMontages and converted them to regular AnimNotifies.
        MONTAGE_BRANCHING_POINT_REMOVAL,
        // Enforce const-correctness in Blueprint implementations of native C++ const class methods
        BLUEPRINT_ENFORCE_CONST_IN_FUNCTION_OVERRIDES,
        // Added pivot to widget components, need to load old versions as a 0,0 pivot, new default is 0.5,0.5
        ADD_PIVOT_TO_WIDGET_COMPONENT,
        // Added finer control over when AI Pawns are automatically possessed. Also renamed Pawn.AutoPossess to Pawn.AutoPossessPlayer indicate this was a setting for players and not AI.
        PAWN_AUTO_POSSESS_AI,
        // Added serialization of timezone to FTextHistory for AsDate operations.
        FTEXT_HISTORY_DATE_TIMEZONE,
        // Sort ActiveBoneIndices on lods so that we can avoid doing it at run time
        SORT_ACTIVE_BONE_INDICES,
        // Added per-frame material uniform expressions
        PERFRAME_MATERIAL_UNIFORM_EXPRESSIONS,
        // Make MikkTSpace the default tangent space calculation method for static meshes.
        MIKKTSPACE_IS_DEFAULT,
        // Only applies to cooked files, grass cooking support.
        LANDSCAPE_GRASS_COOKING,
        // Fixed code for using the bOrientMeshEmitters property.
        FIX_SKEL_VERT_ORIENT_MESH_PARTICLES,
        // Do not change landscape section offset on load under world composition
        LANDSCAPE_STATIC_SECTION_OFFSET,
        // New options for navigation data runtime generation (static, modifiers only, dynamic)
        ADD_MODIFIERS_RUNTIME_GENERATION,
        // Tidied up material's handling of masked blend mode.
        MATERIAL_MASKED_BLENDMODE_TIDY,
        // Original version of MERGED_ADD_MODIFIERS_RUNTIME_GENERATION_TO_4_7; renumbered to prevent blocking promotion in main.
        MERGED_ADD_MODIFIERS_RUNTIME_GENERATION_TO_4_7_DEPRECATED,
        // Original version of AFTER_MERGED_ADD_MODIFIERS_RUNTIME_GENERATION_TO_4_7; renumbered to prevent blocking promotion in main.
        AFTER_MERGED_ADD_MODIFIERS_RUNTIME_GENERATION_TO_4_7_DEPRECATED,
        // After merging ADD_MODIFIERS_RUNTIME_GENERATION into 4.7 branch
        MERGED_ADD_MODIFIERS_RUNTIME_GENERATION_TO_4_7,
        // After merging ADD_MODIFIERS_RUNTIME_GENERATION into 4.7 branch
        AFTER_MERGING_ADD_MODIFIERS_RUNTIME_GENERATION_TO_4_7,
        // Landscape grass weightmap data is now generated in the editor and serialized.
        SERIALIZE_LANDSCAPE_GRASS_DATA,
        // New property to optionally prevent gpu emitters clearing existing particles on Init().
        OPTIONALLY_CLEAR_GPU_EMITTERS_ON_INIT,
        // Also store the Material guid with the landscape grass data
        SERIALIZE_LANDSCAPE_GRASS_DATA_MATERIAL_GUID,
        // Make sure that all template components from blueprint generated classes are flagged as public
        BLUEPRINT_GENERATED_CLASS_COMPONENT_TEMPLATES_PUBLIC,
        // Split out creation method on ActorComponents to distinguish between native, instance, and simple or user construction script
        ACTOR_COMPONENT_CREATION_METHOD,
        // K2Node_Event now uses FMemberReference for handling references
        K2NODE_EVENT_MEMBER_REFERENCE,
        // FPropertyTag stores GUID of struct
        STRUCT_GUID_IN_PROPERTY_TAG,
        // Remove unused UPolys from UModel cooked content
        REMOVE_UNUSED_UPOLYS_FROM_UMODEL,
        // This doesn't do anything except trigger a rebuild on HISMC cluster trees, in this case to get a good "occlusion query" level
        REBUILD_HIERARCHICAL_INSTANCE_TREES,
        // Package summary includes an CompatibleWithEngineVersion field, separately to the version it's saved with
        PACKAGE_SUMMARY_HAS_COMPATIBLE_ENGINE_VERSION,
        // Track UCS modified properties on Actor Components
        TRACK_UCS_MODIFIED_PROPERTIES,
        // Allowed landscape spline meshes to be stored into landscape streaming levels rather than the spline's level
        LANDSCAPE_SPLINE_CROSS_LEVEL_MESHES,
        // Deprecate the variables used for sizing in the designer on UUserWidget
        DEPRECATE_USER_WIDGET_DESIGN_SIZE,
        // Make the editor views array dynamically sized
        ADD_EDITOR_VIEWS,
        // Updated foliage to work with either FoliageType assets or blueprint classes
        FOLIAGE_WITH_ASSET_OR_CLASS,
        // Allows PhysicsSerializer to serialize shapes and actors for faster load times
        BODYINSTANCE_BINARY_SERIALIZATION,
        // Added fastcall data serialization directly in UFunction
        SERIALIZE_BLUEPRINT_EVENTGRAPH_FASTCALLS_IN_UFUNCTION,
        // Changes to USplineComponent and FInterpCurve
        INTERPCURVE_SUPPORTS_LOOPING,
        // Material Instances overriding base material LOD transitions
        MATERIAL_INSTANCE_BASE_PROPERTY_OVERRIDES_DITHERED_LOD_TRANSITION,
        // Serialize ES2 textures separately rather than overwriting the properties used on other platforms
        SERIALIZE_LANDSCAPE_ES2_TEXTURES,
        // Constraint motor velocity is broken into per-component
        CONSTRAINT_INSTANCE_MOTOR_FLAGS,
        // Serialize bIsConst in FEdGraphPinType
        SERIALIZE_PINTYPE_CONST,
        // Change UMaterialFunction::LibraryCategories to LibraryCategoriesText (old assets were saved before auto-conversion of FArrayProperty was possible)
        LIBRARY_CATEGORIES_AS_FTEXT,
        // Check for duplicate exports while saving packages.
        SKIP_DUPLICATE_EXPORTS_ON_SAVE_PACKAGE,
        // Pre-gathering of gatherable, localizable text in packages to optimize text gathering operation times
        SERIALIZE_TEXT_IN_PACKAGES,
        // Added pivot to widget components, need to load old versions as a 0,0 pivot, new default is 0.5,0.5
        ADD_BLEND_MODE_TO_WIDGET_COMPONENT,
        // Added lightmass primitive setting
        NEW_LIGHTMASS_PRIMITIVE_SETTING,
        // Deprecate NoZSpring property on spring nodes to be replaced with TranslateZ property
        REPLACE_SPRING_NOZ_PROPERTY,
        // Keep enums tight and serialize their values as pairs of FName and value. Don't insert dummy values.
        TIGHTLY_PACKED_ENUMS,
        // Changed Asset import data to serialize file meta data as JSON
        ASSET_IMPORT_DATA_AS_JSON,
        // Legacy gamma support for textures.
        TEXTURE_LEGACY_GAMMA,
        // Added WithSerializer for basic native structures like FVector, FColor etc to improve serialization performance
        ADDED_NATIVE_SERIALIZATION_FOR_IMMUTABLE_STRUCTURES,
        // Deprecated attributes that override the style on UMG widgets
        DEPRECATE_UMG_STYLE_OVERRIDES,
        // Shadowmap penumbra size stored
        STATIC_SHADOWMAP_PENUMBRA_SIZE,
        // Fix BC on Niagara effects from the data object and dev UI changes.
        NIAGARA_DATA_OBJECT_DEV_UI_FIX,
        // Fixed the default orientation of widget component so it faces down +x
        FIXED_DEFAULT_ORIENTATION_OF_WIDGET_COMPONENT,
        // Removed bUsedWithUI flag from UMaterial and replaced it with a new material domain for UI
        REMOVED_MATERIAL_USED_WITH_UI_FLAG,
        // Added braking friction separate from turning friction.
        CHARACTER_MOVEMENT_ADD_BRAKING_FRICTION,
        // Removed TTransArrays from UModel
        BSP_UNDO_FIX,
        // Added default value to dynamic parameter.
        DYNAMIC_PARAMETER_DEFAULT_VALUE,
        // Added ExtendedBounds to StaticMesh
        STATIC_MESH_EXTENDED_BOUNDS,
        // Added non-linear blending to anim transitions, deprecating old types
        ADDED_NON_LINEAR_TRANSITION_BLENDS,
        // AO Material Mask texture
        AO_MATERIAL_MASK,
        // Replaced navigation agents selection with single structure
        NAVIGATION_AGENT_SELECTOR,
        // Mesh particle collisions consider particle size.
        MESH_PARTICLE_COLLISIONS_CONSIDER_PARTICLE_SIZE,
        // Adjacency buffer building no longer automatically handled based on triangle count, user-controlled
        BUILD_MESH_ADJ_BUFFER_FLAG_EXPOSED,
        // Change the default max angular velocity
        MAX_ANGULAR_VELOCITY_DEFAULT,
        // Build Adjacency index buffer for clothing tessellation
        APEX_CLOTH_TESSELLATION,
        // Added DecalSize member, solved backward compatibility
        DECAL_SIZE,
        // Keep only package names in StringAssetReferencesMap
        KEEP_ONLY_PACKAGE_NAMES_IN_STRING_ASSET_REFERENCES_MAP,
        // Support sound cue not saving out editor only data
        COOKED_ASSETS_IN_EDITOR_SUPPORT,
        // Updated dialogue wave localization gathering logic.
        DIALOGUE_WAVE_NAMESPACE_AND_CONTEXT_CHANGES,
        // Renamed MakeRot MakeRotator and rearranged parameters.
        MAKE_ROT_RENAME_AND_REORDER,
        // K2Node_Variable will properly have the VariableReference Guid set if available
        K2NODE_VAR_REFERENCEGUIDS,
        // Added support for sound concurrency settings structure and overrides
        SOUND_CONCURRENCY_PACKAGE,
        // Changing the default value for focusable user widgets to false
        USERWIDGET_DEFAULT_FOCUSABLE_FALSE,
        // Custom event nodes implicitly set 'const' on array and non-array pass-by-reference input params
        BLUEPRINT_CUSTOM_EVENT_CONST_INPUT,
        // Renamed HighFrequencyGain to LowPassFilterFrequency
        USE_LOW_PASS_FILTER_FREQ,
        // UAnimBlueprintGeneratedClass can be replaced by a dynamic class. Use TSubclassOf<UAnimInstance> instead.
        NO_ANIM_BP_CLASS_IN_GAMEPLAY_CODE,
        // The SCS keeps a list of all nodes in its hierarchy rather than recursively building it each time it is requested
        SCS_STORES_ALLNODES_ARRAY,
        // Moved StartRange and EndRange in UFbxAnimSequenceImportData to use FInt32Interval
        FBX_IMPORT_DATA_RANGE_ENCAPSULATION,
        // Adding a new root scene component to camera component
        CAMERA_COMPONENT_ATTACH_TO_ROOT,
        // Updating custom material expression nodes for instanced stereo implementation
        INSTANCED_STEREO_UNIFORM_UPDATE,
        // Texture streaming min and max distance to handle HLOD
        STREAMABLE_TEXTURE_MIN_MAX_DISTANCE,
        // Fixing up invalid struct-to-struct pin connections by injecting available conversion nodes
        INJECT_BLUEPRINT_STRUCT_PIN_CONVERSION_NODES,
        // Saving tag data for Array Property's inner property
        INNER_ARRAY_TAG_INFO,
        // Fixed duplicating slot node names in skeleton due to skeleton preload on compile
        FIX_SLOT_NAME_DUPLICATION,
        // Texture streaming using AABBs instead of Spheres
        STREAMABLE_TEXTURE_AABB,
        // FPropertyTag stores GUID of property
        PROPERTY_GUID_IN_PROPERTY_TAG,
        // Name table hashes are calculated and saved out rather than at load time
        NAME_HASHES_SERIALIZED,
        // Updating custom material expression nodes for instanced stereo implementation refactor
        INSTANCED_STEREO_UNIFORM_REFACTOR,
        // Added compression to the shader resource for memory savings
        COMPRESSED_SHADER_RESOURCES,
        // Cooked files contain the dependency graph for the event driven loader (the serialization is largely independent of the use of the new loader)
        PRELOAD_DEPENDENCIES_IN_COOKED_EXPORTS,
        // Cooked files contain the TemplateIndex used by the event driven loader (the serialization is largely independent of the use of the new loader, i.e. this will be null if cooking for the old loader)
        TemplateIndex_IN_COOKED_EXPORTS,
        // FPropertyTag includes contained type(s) for Set and Map properties:
        PROPERTY_TAG_SET_MAP_SUPPORT,
        // Added SearchableNames to the package summary and asset registry
        ADDED_SEARCHABLE_NAMES,
        // Increased size of SerialSize and SerialOffset in export map entries to 64 bit, allow support for bigger files
        e64BIT_EXPORTMAP_SERIALSIZES,
        // Sky light stores IrradianceMap for mobile renderer.
        SKYLIGHT_MOBILE_IRRADIANCE_MAP,
        // Added flag to control sweep behavior while walking in UCharacterMovementComponent.
        ADDED_SWEEP_WHILE_WALKING_FLAG,
        // StringAssetReference changed to SoftObjectPath and swapped to serialize as a name+string instead of a string
        ADDED_SOFT_OBJECT_PATH,
        // Changed the source orientation of point lights to match spot lights (z axis)
        POINTLIGHT_SOURCE_ORIENTATION,
        // LocalizationId has been added to the package summary (editor-only)
        ADDED_PACKAGE_SUMMARY_LOCALIZATION_ID,
        // Fixed case insensitive hashes of wide strings containing character values from 128-255
        FIX_WIDE_STRING_CRC,
        // Added package owner to allow private references
        ADDED_PACKAGE_OWNER,
        // Changed the data layout for skin weight profile data
        SKINWEIGHT_PROFILE_DATA_LAYOUT_CHANGES,
        // Added import that can have package different than their outer
        NON_OUTER_PACKAGE_IMPORT,
        // Added DependencyFlags to AssetRegistry
        ASSETREGISTRY_DEPENDENCYFLAGS,
        // Fixed corrupt licensee flag in 4.26 assets
        CORRECT_LICENSEE_FLAG,

        // -----<new versions can be added before this line>-------------------------------------------------
        // - this needs to be the last line (see note below)
        AUTOMATIC_VERSION_PLUS_ONE,
        AUTOMATIC_VERSION = AUTOMATIC_VERSION_PLUS_ONE - 1
    }

    public enum EUnrealEngineObjectLicenseeUEVersion
    {
        VER_LIC_NONE = 0,

        // - this needs to be the last line (see note below)
        VER_LIC_AUTOMATIC_VERSION_PLUS_ONE,
        VER_LIC_AUTOMATIC_VERSION = VER_LIC_AUTOMATIC_VERSION_PLUS_ONE - 1
    }

    /// <summary>
    /// This object combines all of our version enums into a single easy to use structure
    /// which allows us to update older version numbers independently of the newer version numbers.
    /// </summary>
    public struct FPackageFileVersion : IComparable<EUnrealEngineObjectUE4Version>, IComparable<EUnrealEngineObjectUE5Version>
    {
        /// UE4 file version
        public int FileVersionUE4;

        /// UE5 file version
        public int FileVersionUE5;

        /// Set all versions to the default state
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            FileVersionUE4 = 0;
            FileVersionUE5 = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FPackageFileVersion(int ue4Version, int ue5Version)
        {
            FileVersionUE4 = ue4Version;
            FileVersionUE5 = ue5Version;
        }

        /// Creates and returns a FPackageFileVersion based on a single EUnrealEngineObjectUEVersion and no other versions.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FPackageFileVersion CreateUE4Version(int version) => new(version, 0);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FPackageFileVersion CreateUE4Version(EUnrealEngineObjectUE4Version version) => new((int) version, 0);

        public int Value
        {
            get => FileVersionUE5 >= (int) EUnrealEngineObjectUE5Version.INITIAL_VERSION ? FileVersionUE5 : FileVersionUE4;
            set
            {
                if (value >= (int) EUnrealEngineObjectUE5Version.INITIAL_VERSION)
                {
                    FileVersionUE5 = value;
                }
                else
                {
                    FileVersionUE4 = value;
                }
            }
        }

        /// UE4 version comparisons
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(FPackageFileVersion a, EUnrealEngineObjectUE4Version b) => a.FileVersionUE4 == (int) b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(FPackageFileVersion a, EUnrealEngineObjectUE4Version b) => a.FileVersionUE4 != (int) b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(FPackageFileVersion a, EUnrealEngineObjectUE4Version b) => a.FileVersionUE4 < (int) b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(FPackageFileVersion a, EUnrealEngineObjectUE4Version b) => a.FileVersionUE4 > (int) b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(FPackageFileVersion a, EUnrealEngineObjectUE4Version b) => a.FileVersionUE4 <= (int) b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(FPackageFileVersion a, EUnrealEngineObjectUE4Version b) => a.FileVersionUE4 >= (int) b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(EUnrealEngineObjectUE4Version other) => FileVersionUE4.CompareTo(other);

        /// UE5 version comparisons
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(FPackageFileVersion a, EUnrealEngineObjectUE5Version b) => a.FileVersionUE5 == (int) b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(FPackageFileVersion a, EUnrealEngineObjectUE5Version b) => a.FileVersionUE5 != (int) b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(FPackageFileVersion a, EUnrealEngineObjectUE5Version b) => a.FileVersionUE5 < (int) b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(FPackageFileVersion a, EUnrealEngineObjectUE5Version b) => a.FileVersionUE5 > (int) b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(FPackageFileVersion a, EUnrealEngineObjectUE5Version b) => a.FileVersionUE5 <= (int) b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(FPackageFileVersion a, EUnrealEngineObjectUE5Version b) => a.FileVersionUE5 >= (int) b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(EUnrealEngineObjectUE5Version other) => FileVersionUE5.CompareTo(other);

        /// <summary>
        /// Returns true if this object is compatible with the FPackageFileVersion passed in as the parameter.
        /// This means that  all version numbers for the current object are equal or greater than the
        /// corresponding version numbers of the other structure.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsCompatible(FPackageFileVersion other) => FileVersionUE4 >= other.FileVersionUE4 && FileVersionUE5 >= other.FileVersionUE5;

        /// FPackageFileVersion comparisons
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(FPackageFileVersion a, FPackageFileVersion b) => a.FileVersionUE4 == b.FileVersionUE4 && a.FileVersionUE5 == b.FileVersionUE5;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(FPackageFileVersion a, FPackageFileVersion b) => !(a == b);
        public override bool Equals(object? obj) => obj is FPackageFileVersion other && this == other;
        public override int GetHashCode() => HashCode.Combine(FileVersionUE4, FileVersionUE5);

        public override string ToString()
            => FileVersionUE5 >= (int) EUnrealEngineObjectUE5Version.INITIAL_VERSION
                ? ((EUnrealEngineObjectUE5Version) FileVersionUE5).ToString()
                : ((EUnrealEngineObjectUE4Version) FileVersionUE4).ToString();
    }
}
