#include "Structs.h"


struct MeshVertex
{
    FVector Position;
    FVector Normal;
    FVector Tangent;
    FUVFloat UV;
};

struct MeshSection
{
    int MaterialIndex;
    int FirstIndex;
    int NumFaces;
    TArray<char> MaterialName;
};


struct BaseMeshLod {
    int NumVertices;
    int NumTexCoords;
    TArray<MeshSection> Sections;
    TArray<TArray<FUVFloat>> ExtraUVs;
    TArray<FColor> VertexColors;
    TArray<int> Indices;
};


struct StaticMeshLod : public BaseMeshLod {
    TArray<MeshVertex> Vertices;
};
