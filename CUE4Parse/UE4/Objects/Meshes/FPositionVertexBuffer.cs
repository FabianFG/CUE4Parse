using System;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Meshes;

[JsonConverter(typeof(FPositionVertexBufferConverter))]
public class FPositionVertexBuffer
{
    public FVector[] Verts;
    public int Stride;
    public int NumVertices;

    public FPositionVertexBuffer()
    {
        Verts = [];
    }

    public FPositionVertexBuffer(FArchive Ar)
    {
        if (Ar.Game is EGame.GAME_Undawn or EGame.GAME_RacingMaster)
        {
            bool bUseFullPrecisionPositions = Ar.Game == EGame.GAME_Undawn && Ar.ReadBoolean();
            Stride = Ar.Read<int>();
            NumVertices = Ar.Read<int>();
            bUseFullPrecisionPositions = Ar.Game == EGame.GAME_RacingMaster && Stride == 12;
            Verts = bUseFullPrecisionPositions ? Ar.ReadBulkArray<FVector>() : Ar.ReadBulkArray<FVector>(() => Ar.Read<FVector3UnsignedShort>());
            return;
        }
        Stride = Ar.Read<int>();
        NumVertices = Ar.Read<int>();
        if (Ar.Game == EGame.GAME_Valorant_PRE_11_2)
        {
            bool bUseFullPrecisionPositions = Ar.ReadBoolean();
            var bounds = new FBoxSphereBounds(Ar);
            if (!bUseFullPrecisionPositions)
            {
                var vertsHalf = Ar.ReadBulkArray<FVector3SignedShortScale>();
                Verts = new FVector[vertsHalf.Length];
                for (int i = 0; i < vertsHalf.Length; i++)
                    Verts[i] = vertsHalf[i] * bounds.BoxExtent + bounds.Origin;
                return;
            }
        }
        if (Ar.Game is EGame.GAME_Gothic1Remake && Stride == 8)
        {
            var vertsHalf = Ar.ReadBulkArray<FHalfVector4>();
            Verts = new FVector[vertsHalf.Length];
            for (int i = 0; i < vertsHalf.Length; i++)
                Verts[i] = vertsHalf[i];
            return;
        }
        if (Ar.Game is EGame.GAME_DaysGone)
        {
            Verts = Stride switch
            {
                4 => Ar.ReadBulkArray(() => (FVector) Ar.Read<FVector3Packed32>()),
                8 => Ar.ReadBulkArray(() => (FVector) Ar.Read<FVector3UnsignedShortScale>()),
                12 => Ar.ReadBulkArray<FVector>(),
                _ => throw new ArgumentOutOfRangeException($"Unknown stride {Stride} for FPositionVertexBuffer")
            };
            return;
        }
        if (Ar.Game == EGame.GAME_FateTrigger)
        {
            var box = Ar.Read<byte>();
            Verts = Ar.ReadBulkArray<FVector>();
            if (box != 0)
            {
                Ar.Position += 24; // Box
                Ar.SkipBulkArrayData();
            }
            return;
        }
        if (Ar.Game == EGame.GAME_Gollum) Ar.Position += 25;

        Verts = Ar.ReadBulkArray<FVector>();
    }
}
