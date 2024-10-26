using CUE4Parse.MappingsProvider.Usmap;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject;

public class UCustomizableObject : UObject
{
	public ECustomizableObjectVersions Version;
	public Model Model;

	public override void Deserialize(FAssetArchive Ar, long validPos)
	{
		base.Deserialize(Ar, validPos);

		Version = Ar.Read<ECustomizableObjectVersions>();
		Model = new Model(Ar);
	}
}

public enum ECustomizableObjectVersions
{
    FirstEnumeratedVersion = 450,

    DeterminisiticMeshVertexIds,

    NumRuntimeReferencedTextures,
		
    DeterminisiticLayoutBlockIds,

    BackoutDeterminisiticLayoutBlockIds,

    FixWrappingProjectorLayoutBlockId,

    MeshReferenceSupport,

    ImproveMemoryUsageForStreamableBlocks,

    FixClipMeshWithMeshCrash,

    SkeletalMeshLODSettingsSupport,

    RemoveCustomCurve,

    AddEditorGamePlayTags,

    AddedParameterThumbnailsToEditor,

    ComponentsLODsRedesign,

    ComponentsLODsRedesign2,

    LayoutToPOD,

    AddedRomFlags,

    LayoutNodeCleanup,

    AddSurfaceAndMeshMetadata,

    TablesPropertyNameBug,

    DataTablesParamTrackingForCompileOnlySelected,

    CompilationOptimizationsMeshFormat,

    ModelStreamableBulkData,

    LayoutBlocksAsInt32,
		
    IntParameterOptionDataTable,

    RemoveLODCountLimit,

    IntParameterOptionDataTablePartialBackout,

    IntParameterOptionDataTablePartialRestore,

    CorrectlySerializeTableToParamNames,
		
    AddMaterialSlotNameIndexToSurfaceMetadata,

    NodeComponentMesh,
		
    MoveEditNodesToModifiers,

    DerivedDataCache,

    ComponentsArray,

    FixComponentNames,

    AddedFaceCullStrategyToSomeOperations,

    DDCParticipatingObjects,

    GroupRomsBySource,
		
    RemovedGroupRomsBySource,

    ReGroupRomsBySource,

    UIMetadataGameplayTags,

    TransformInMeshModifier,
		
    SurfaceMetadataSlotNameIndexToName,

    BulkDataFilesNumFilesLimit,

    RemoveModifiersHack,

    SurfaceMetadataSerialized,

    FixesForMeshSectionMultipleOutputs,

    // -----<new versions can be added above this line>--------
    LastCustomizableObjectVersion
}