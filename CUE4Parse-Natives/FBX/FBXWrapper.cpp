static_assert(WITH_FBX, "hmmmm whatchu doing");
#include "Framework.h"
#include "MeshIO.h"
#include "FBXIOStream.h"
#include <fbxsdk.h>
#include <cassert>

//DLLEXPORT void TESTMeshSetionMarshal(TArray<MeshSection> sectionArray)
//{
//	//assert(test != nullptr);
//	assert(sectionArray.Count == 1);
//	MeshSection section = sectionArray[0];
//	assert(section.MaterialIndex == 768);
//	assert(section.FirstIndex == 0);
//	assert(section.NumFaces == 460);
//	//assert(section.MaterialName.GetData() == "mat");
//}

FbxManager* SDKManagerInstance = nullptr;

FbxManager* GetFbxManager() {
	if (SDKManagerInstance == nullptr)
	{
		SDKManagerInstance = FbxManager::Create();
		FbxIOSettings* ios = FbxIOSettings::Create(SDKManagerInstance, IOSROOT);
		SDKManagerInstance->SetIOSettings(ios);
	}
	return SDKManagerInstance;
}

DLLEXPORT void* CreateScene()
{
	FbxScene* Scene = FbxScene::Create(GetFbxManager(), "");

	// create scene info
	FbxDocumentInfo* SceneInfo = FbxDocumentInfo::Create(GetFbxManager(), "SceneInfo");
	SceneInfo->mTitle = "CUE4Parse Exporter";
	SceneInfo->mSubject = "";
	SceneInfo->mAuthor = "";
	SceneInfo->mRevision = "";
	SceneInfo->mKeywords = "";
	SceneInfo->mComment = "";
	Scene->SetSceneInfo(SceneInfo);

	Scene->GetGlobalSettings().SetSystemUnit(FbxSystemUnit::cm);
	return Scene;
}

FbxNode* CreateStaticMeshNode(char* name, StaticMeshLod lod, bool bWeldVerts)
{
	FbxNode* Node = FbxNode::Create(GetFbxManager(), name);
	FbxMesh* Mesh = FbxMesh::Create(GetFbxManager(), name);


	TArray<int> VertRemap;
	TArray<int> UniqueVerts;

	if (bWeldVerts)
	{
		// TODO
	} else
	{
		VertRemap.AddUninitialized(lod.NumVertices);
		for (int i = 0; i < VertRemap.Count; i++)
		{
			VertRemap[i] = i;
		}
		UniqueVerts = VertRemap;
	}
	
	const int VertexCount = VertRemap.Count;
	const int PolygonsCount = lod.Sections.Count;

	// create control points
	Mesh->InitControlPoints(lod.NumVertices);
	FbxVector4* ControlPoints = Mesh->GetControlPoints();
	for (int i = 0; i < UniqueVerts.Count; i++)
	{
		int VertIndex = UniqueVerts[i];
		MeshVertex vert = lod.Vertices[VertIndex];
		ControlPoints[i] = FbxVector4(vert.Position.X, -vert.Position.Y, vert.Position.Z);
	}

	// normal set to layer 0
	FbxLayer* Layer = Mesh->GetLayer(0);
	if (Layer == nullptr)
	{
		Mesh->CreateLayer();
		Layer = Mesh->GetLayer(0);
	}

	TArray<unsigned int> Indices;
	Indices.Reserve(lod.NumVertices / 3);
	for (int i = 0; i < lod.Sections.Count; i++)
	{
		MeshSection section = lod.Sections[i];
		for (int FaceIndex = 0; FaceIndex < section.NumFaces; FaceIndex++)
		{
			auto wedgeIndex = new unsigned int[3];
			for (int PointIndex = 0; PointIndex < 3; PointIndex++)
			{
				// UnrealVertIndex 
				int index = lod.Indices[section.FirstIndex + ((FaceIndex * 3) + PointIndex)];
				Indices.Add(index);
			}
		}
	}

	FbxLayerElementNormal* LayerElementNormal = FbxLayerElementNormal::Create(Mesh, "");
	FbxLayerElementTangent* LayerElementTangent = FbxLayerElementTangent::Create(Mesh, "");
	FbxLayerElementBinormal* LayerElementBinormal = FbxLayerElementBinormal::Create(Mesh, "");

	LayerElementNormal->SetMappingMode(FbxLayerElement::eByPolygonVertex);
	LayerElementTangent->SetMappingMode(FbxLayerElement::eByPolygonVertex);
	LayerElementBinormal->SetMappingMode(FbxLayerElement::eByPolygonVertex);

	LayerElementNormal->SetReferenceMode(FbxLayerElement::eDirect);
	LayerElementTangent->SetReferenceMode(FbxLayerElement::eDirect);
	LayerElementBinormal->SetReferenceMode(FbxLayerElement::eDirect);


	TArray<FbxVector4> FbxNormals;
	TArray<FbxVector4> FbxTangents;
	TArray<FbxVector4> FbxBinormals;

	FbxNormals.AddUninitialized(VertexCount);
	FbxTangents.AddUninitialized(VertexCount);
	FbxBinormals.AddUninitialized(VertexCount);

	for (int i = 0; i < VertexCount; i++)
	{
		MeshVertex vert = lod.Vertices[i];
		FbxVector4 Normal = FbxVector4(vert.Normal.X, -vert.Normal.Y, vert.Normal.Z);
		Normal.Normalize();
		FbxNormals[i] = Normal;

		FbxVector4 Tangent = FbxVector4(vert.Tangent.X, -vert.Tangent.Y, vert.Tangent.Z);
		Tangent.Normalize();
		FbxTangents[i] = Tangent;

		FbxVector4 Binormal = FbxVector4(vert.Tangent.X, -vert.Tangent.Y, vert.Tangent.Z);
		Binormal.Normalize();
		FbxBinormals[i] = Binormal;
	}
	
	for (int i = 0; i < Indices.Count; i++)
	{
		uint VertIndex = Indices[i];
		LayerElementNormal->GetDirectArray().Add(FbxNormals[VertIndex]);
		LayerElementTangent->GetDirectArray().Add(FbxTangents[VertIndex]);
		LayerElementBinormal->GetDirectArray().Add(FbxBinormals[VertIndex]);
	}

	Layer->SetNormals(LayerElementNormal);
	Layer->SetTangents(LayerElementTangent);
	Layer->SetBinormals(LayerElementBinormal);


	FbxNormals.Clear();
	FbxTangents.Clear();
	FbxBinormals.Clear();

	int NumTextCoords = lod.ExtraUVs.Count + 1;
	for (int i = 0; i < NumTextCoords; i++)
	{
		auto UVChannelName = "UVmap_" + i;
		FbxLayer* UVsLayer = Mesh->GetLayer(i);
		if (UVsLayer == nullptr)
		{
			Mesh->CreateLayer();
			UVsLayer = Mesh->GetLayer(i);
		}
		assert(UVsLayer != nullptr);

		FbxLayerElementUV* UVDiffuseLayer = FbxLayerElementUV::Create(Mesh, UVChannelName);

		UVDiffuseLayer->SetMappingMode(FbxLayerElement::eByPolygonVertex);
		UVDiffuseLayer->SetReferenceMode(FbxLayerElement::eIndexToDirect);

		TArray<int> UvsRemap;
		TArray<int> UniqueUVs;
		if (bWeldVerts)
		{
			// TODO
			// Weld UVs
			//DetermineUVsToWeld(UvsRemap, UniqueUVs, RenderMesh.VertexBuffer, TexCoordSourceIndex);
		}
		else
		{
			// Do not weld UVs
			UvsRemap = VertRemap;
			UniqueUVs = UvsRemap;
		}

		for (int j = 0; j < UniqueUVs.Count; j++)
		{
			int UVIndex = UniqueUVs[j];
			FUVFloat TexCoord;
			if (i == 0) // main uv
			{
				TexCoord = lod.Vertices[UVIndex].UV;
			}
			else
			{
				TexCoord = lod.ExtraUVs[i - 1][UVIndex];
			}

			UVDiffuseLayer->GetDirectArray().Add(FbxVector2(TexCoord.U, -TexCoord.V + 1.0));
		}
		
		UVDiffuseLayer->GetIndexArray().SetCount(Indices.Count);
		for (int j = 0; j < Indices.Count; j++)
		{
			// int VertIndex = Indices[j];
			int UVIndex = UvsRemap[Indices[j]];
			UVDiffuseLayer->GetIndexArray().SetAt(j, UVIndex);
		}

		UVsLayer->SetUVs(UVDiffuseLayer, FbxLayerElement::eTextureDiffuse);
		UvsRemap.Clear();
		UniqueUVs.Clear();
	}

	FbxLayerElementMaterial* MatLayer = FbxLayerElementMaterial::Create(Mesh, "");
	MatLayer->SetMappingMode(FbxLayerElement::eByPolygon);
	MatLayer->SetReferenceMode(FbxLayerElement::eIndexToDirect);
	Layer->SetMaterials(MatLayer);

	for (int i = 0; i < lod.Sections.Count; i++)
	{
		MeshSection section = lod.Sections[i];

		FbxSurfaceMaterial* FbxMaterial = FbxSurfaceLambert::Create(GetFbxManager(), section.MaterialName.GetData());
		int MatIndex = Node->AddMaterial(FbxMaterial);


		for (int FaceIndex = 0; FaceIndex < section.NumFaces; FaceIndex++)
		{
			Mesh->BeginPolygon(MatIndex);
			for (int PointIndex = 0; PointIndex < 3; PointIndex++)
			{
				uint VertIndex = lod.Indices[section.FirstIndex + ((FaceIndex * 3) + PointIndex)];
				int RemappedVertIndex = VertRemap[VertIndex];
				Mesh->AddPolygon(RemappedVertIndex);
			}
			Mesh->EndPolygon();
		}
	}

	if (lod.VertexColors.Count > 0)
	{
		FbxLayerElementVertexColor* VertexColor = FbxLayerElementVertexColor::Create(Mesh, "");
		VertexColor->SetMappingMode(FbxLayerElement::eByPolygonVertex);
		VertexColor->SetReferenceMode(FbxLayerElement::eIndexToDirect);
		FbxLayerElementArrayTemplate<FbxColor>& VertexColorArray = VertexColor->GetDirectArray();
		Layer->SetVertexColors(VertexColor);

		for (int VertIndex = 0; VertIndex < Indices.Count; VertIndex++) // remap??
		{
			FLinearColor VertColor = { 1.0f, 1.0f, 1.0f };
			uint UnrealVertIndex = Indices[VertIndex];
			if ((int)UnrealVertIndex < lod.VertexColors.Count)
			{
				VertColor = lod.VertexColors[UnrealVertIndex].ReinterpretAsLinear();
			}
			VertexColorArray.Add(FbxColor(VertColor.R, VertColor.G, VertColor.B, VertColor.A));
		}

		VertexColor->GetIndexArray().SetCount(Indices.Count);
		for (int i = 0; i < Indices.Count; i++)
		{
			VertexColor->GetIndexArray().SetAt(i, i);
		}
	}

	VertRemap.Clear();
	UniqueVerts.Clear();
	Indices.Clear();

	Node->SetNodeAttribute(Mesh);
	return Node;
}

DLLEXPORT void* CreateStaticMesh(char* name, StaticMeshLod lod, bool bWeldVerts)
{
	FbxScene* Scene = (FbxScene*)CreateScene();
	FbxNode* Node = (FbxNode*)CreateStaticMeshNode(name, lod, bWeldVerts);
	Scene->GetRootNode()->AddChild(Node);
	return Scene;
}

DLLEXPORT void* SaveScene(void* Scene, int FBXFileVersion /*7700*/)
{
	// save to 
	FbxExporter* Exporter = FbxExporter::Create(GetFbxManager(), "");
	
	if (!Exporter->SetFileExportVersion(FBXFileVersion))
	{
		printf("Invalid FBX file version number\n");
		return nullptr;
	}

	int FileFormat = -1;
	/*if (bASCII)
	{
		FileFormat = GetFbxManager()->GetIOPluginRegistry()->FindWriterIDByDescription("FBX ascii (*.fbx)");
	}
	else*/
	{
		FileFormat = GetFbxManager()->GetIOPluginRegistry()->GetNativeWriterFormat();
	}
	
	Exporter->Initialize("", FileFormat, GetFbxManager()->GetIOSettings());

	MyFbxStream Stream = MyFbxStream(GetFbxManager());
	
	//Exporter->Initialize("cube2.fbx", FileFormat, GetFbxManager()->GetIOSettings());

	Exporter->Initialize(&Stream, nullptr, FileFormat, GetFbxManager()->GetIOSettings());

	Exporter->Export(static_cast<FbxScene*>(Scene));

	auto StreamData = Stream.GetStreamData();
	Exporter->Destroy();

	return StreamData;
}
