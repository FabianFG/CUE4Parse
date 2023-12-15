using System.Collections.Generic;
using CUE4Parse_Conversion.UEFormat;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Meshes.UEFormat;

public class FMorphTarget : ISerializable
{
    private readonly FString MorphName;
    private readonly List<FMorphData> MorphData = new();
    
    public FMorphTarget(string morphName, FMorphTargetLODModel morphLod)
    {
        MorphName = new FString(morphName);
        foreach (var delta in morphLod.Vertices)
        {
            MorphData.Add(new FMorphData(delta.PositionDelta, delta.TangentZDelta, delta.SourceIdx));
        }
    }
    
    public void Serialize(FArchiveWriter Ar)
    {
        MorphName.Serialize(Ar);
        Ar.WriteArray(MorphData);
    }
}

public class FMorphData : ISerializable
{
    private readonly FVector PositionDelta;
    private readonly FVector TangentZDelta;
    private readonly uint VertexIndex;

    public FMorphData(FVector positionDelta, FVector tangentZDelta, uint vertexIndex)
    {
        PositionDelta = positionDelta;
        PositionDelta.Y = -PositionDelta.Y;
        
        TangentZDelta = tangentZDelta;
        TangentZDelta.Y = -TangentZDelta.Y;
        
        VertexIndex = vertexIndex;
    }
    
    public void Serialize(FArchiveWriter Ar)
    {
        PositionDelta.Serialize(Ar);
        TangentZDelta.Serialize(Ar);
        Ar.Write(VertexIndex);
    }
}