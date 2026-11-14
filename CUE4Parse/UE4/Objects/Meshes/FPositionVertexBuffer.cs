using CUE4Parse.GameTypes.SuicideSquad.Objects;
using CUE4Parse.GameTypes.Tencent.GangstarMirageCity.Objects.Meshes;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Meshes;

[JsonConverter(typeof(FPositionVertexBufferConverter))]
public class FPositionVertexBuffer
{
    [JsonIgnore] public FVector[] Verts { get; protected set; } = null!;
    public int Stride { get; protected set; }
    public int NumVertices { get; protected set; }

    // For some custom implementations we need bounds, but we don't have them at the time, we can use this callback instead
    private Action<FBoxSphereBounds>? _applyBounds;

    public FPositionVertexBuffer()
    {
        Verts = [];
    }

    public FPositionVertexBuffer(FArchive Ar)
    {
        switch (Ar.Game)
        {
            case GAME_Valorant_PRE_11_2 or GAME_NeedForSpeedMobile:
                ReadPositionsWithInlineBounds(Ar);
                return;
            case GAME_Gothic1Remake or GAME_DestroyAllHumans2Reprobed:
                ReadHalfPrecisionPositions(Ar);
                return;

            case GAME_ArenaBreakoutInfinite or GAME_ArenaBreakoutMobile:
                ReadArenaBreakout(Ar);
                return;
            case GAME_GangstarMirageCity:
                ReadGangstar(Ar);
                return;
            case GAME_Undawn:
                ReadUndawn(Ar);
                return;
            case GAME_RacingMaster:
                ReadRacingMaster(Ar);
                return;
            case GAME_HonorofKingsWorld:
                ReadHonorofKingsWorld(Ar);
                return;
            case GAME_Farlight84:
                ReadFarlight84(Ar);
                return;
            case GAME_SuicideSquad:
                ReadSuicideSquad(Ar);
                return;
            case GAME_TamasShadowveil:
                ReadTamasShadowveil(Ar);
                return;
            case GAME_MonsterHunterOutlanders:
                ReadMonsterHunterOutlanders(Ar);
                return;
            case GAME_Splitgate2 or GAME_Empulse:
                Read1047Games(Ar);
                return;
            case GAME_DaysGone:
                ReadDaysGone(Ar);
                return;
            case GAME_RocoKingdomWorld:
                ReadRocoKingdomWorld(Ar);
                return;
            case GAME_FateTrigger:
                ReadFateTrigger(Ar);
                return;
            case GAME_WorldofJadeDynasty:
                ReadWorldofJadeDynasty(Ar);
                return;
            case GAME_Gollum:
                ReadGollum(Ar);
                return;
            case GAME_GearsofWarEDay:
                ReadGearsofWarEDay(Ar);
                return;
            case GAME_LifeIsStrange:
                ReadLifeIsStrange(Ar);
                return;
        }

        Stride = Ar.Read<int>();
        NumVertices = Ar.Read<int>();
        Verts = Ar.ReadBulkArray<FVector>();
    }

    #region Shared

    internal void ApplyBounds(FBoxSphereBounds bounds) => _applyBounds?.Invoke(bounds);

    private void ReadPositionsWithInlineBounds(FArchive Ar)
    {
        Stride = Ar.Read<int>();
        NumVertices = Ar.Read<int>();
        bool bUseFullPrecisionPositions = Ar.ReadBoolean();
        var bounds = new FBoxSphereBounds(Ar);
        if (bUseFullPrecisionPositions)
        {
            Verts = Ar.ReadBulkArray<FVector>();
        }
        else
        {
            ReadScaledPositions(Ar, bounds.Origin, bounds.BoxExtent);
        }
    }

    private void ReadHalfPrecisionPositions(FArchive Ar)
    {
        Stride = Ar.Read<int>();
        NumVertices = Ar.Read<int>();
        switch (Stride)
        {
            case 8:
            {
                var vertsHalf = Ar.ReadBulkArray<FHalfVector4>();
                Verts = new FVector[vertsHalf.Length];
                for (var i = 0; i < vertsHalf.Length; i++)
                {
                    Verts[i] = vertsHalf[i];
                }
                break;
            }
            default:
                Verts = Ar.ReadBulkArray<FVector>();
                break;
        }
    }

    private void ReadScaledPositions(FArchive Ar, FVector origin, FVector extent)
    {
        switch (Stride)
        {
            case 8:
            {
                var vertsHalf = Ar.ReadBulkArray<FVector3SignedShortScale>();
                Verts = new FVector[vertsHalf.Length];
                for (var i = 0; i < vertsHalf.Length; i++)
                {
                    Verts[i] = vertsHalf[i] * extent + origin;
                }
                break;
            }
            case 12:
                Verts = Ar.ReadBulkArray<FVector>();
                break;
            default:
                throw new ArgumentOutOfRangeException($"Unknown stride {Stride} for FPositionVertexBuffer");
        }
    }
    #endregion

    #region Custom

    private void ReadArenaBreakout(FArchive Ar)
    {
        Stride = Ar.Read<int>();
        NumVertices = Ar.Read<int>();
        if (Stride != 8)
        {
            Verts = Ar.ReadBulkArray<FVector>();
            return;
        }

        var bounds = new FBoxSphereBounds(Ar);
        ReadScaledPositions(Ar, bounds.Origin, bounds.BoxExtent);
    }

    private void ReadGangstar(FArchive Ar)
    {
        var buffer = new FGangstarPositionVertexBuffer(Ar);
        Stride = buffer.Stride;
        NumVertices = buffer.NumVertices;
        Verts = buffer.Verts;
        _applyBounds = buffer.Decode;
    }

    private void ReadUndawn(FArchive Ar)
    {
        bool bUseFullPrecisionPositions = Ar.ReadBoolean();
        Stride = Ar.Read<int>();
        NumVertices = Ar.Read<int>();
        Verts = bUseFullPrecisionPositions ? Ar.ReadBulkArray<FVector>() : Ar.ReadBulkArray<FVector>(() => Ar.Read<FVector3UnsignedShort>());
    }

    private void ReadRacingMaster(FArchive Ar)
    {
        Stride = Ar.Read<int>();
        NumVertices = Ar.Read<int>();
        Verts = Stride switch
        {
            12 => Ar.ReadBulkArray<FVector>(),
            _ => Ar.ReadBulkArray<FVector>(() => Ar.Read<FVector3UnsignedShort>())
        };
    }

    private void ReadHonorofKingsWorld(FArchive Ar)
    {
        var size = Ar.Read<int>();
        switch (size)
        {
            case 32:
            {
                NumVertices = Ar.Read<int>();
                var pos = Ar.Read<FVector>();
                var extent = Ar.Read<FVector>();
                Stride = Ar.Read<int>();
                Ar.Position -= 4;
                Verts = Ar.ReadBulkArray(() => (FVector) Ar.Read<FVector3UnsignedShortScale>());
                for (int i = 0; i < Verts.Length; i++)
                {
                    Verts[i] = Verts[i] * extent / 65536 + pos;
                }
                break;
            }
            case 12:
                Stride = size;
                NumVertices = Ar.Read<int>();
                Verts = Ar.ReadBulkArray<FVector>();
                break;
        }
    }

    private void ReadFarlight84(FArchive Ar)
    {
        bool bUseHalfPrecisionPositions = Ar.ReadBoolean();
        Stride = Ar.Read<int>();
        NumVertices = Ar.Read<int>();
        if (bUseHalfPrecisionPositions)
        {
            _ = Ar.ReadArray<FVector>(2);
            Verts = Ar.ReadBulkArray<FVector>(() => Ar.Read<FVector3UnsignedShort>());
        }
        else
        {
            Verts = Ar.ReadBulkArray<FVector>();
        }
    }

    private void ReadSuicideSquad(FArchive Ar)
    {
        Stride = Ar.Read<int>();
        NumVertices = Ar.Read<int>();
        Ar.Position += 1;

        var vectors = Ar.ReadArray<FVector>(2);
        //second vector is extent - origin
        switch (Stride)
        {
            case 12:
                Verts = Ar.ReadBulkArray<FVector>();
                break;
            default:
            {
                var vertsHalf = Ar.ReadBulkArray<FVectorShort>();
                Verts = new FVector[vertsHalf.Length];
                for (int i = 0; i < vertsHalf.Length; i++)
                {
                    Verts[i] = vertsHalf[i] / vectors[0] - vectors[1];
                }
                break;
            }
        }
    }

    private void ReadTamasShadowveil(FArchive Ar)
    {
        Stride = Ar.Read<int>();
        NumVertices = Ar.Read<int>();
        Ar.Position += 4;
        Verts = Ar.ReadBulkArray<FVector>();
    }

    private void ReadMonsterHunterOutlanders(FArchive Ar)
    {
        Stride = Ar.Read<int>();
        NumVertices = Ar.Read<int>();
        Ar.Position += 4;
        var origin = Ar.Read<FVector>();
        var extent = Ar.Read<FVector>();
        ReadScaledPositions(Ar, origin, extent);
    }

    private void Read1047Games(FArchive Ar)
    {
        Stride = Ar.Read<int>();
        NumVertices = Ar.Read<int>();
        Ar.Position += 1;
        Verts = Stride switch
        {
            8 => Ar.ReadBulkArray<FVector>(() => Ar.Read<FHalfVector4>()),
            _ => Ar.ReadBulkArray<FVector>()
        };
    }

    private void ReadDaysGone(FArchive Ar)
    {
        Stride = Ar.Read<int>();
        NumVertices = Ar.Read<int>();
        Verts = Stride switch
        {
            4 => Ar.ReadBulkArray(() => (FVector) Ar.Read<FVector3Packed32>()),
            8 => Ar.ReadBulkArray(() => (FVector) Ar.Read<FVector3UnsignedShortScale>()),
            12 => Ar.ReadBulkArray<FVector>(),
            _ => throw new ArgumentOutOfRangeException($"Unknown stride {Stride} for FPositionVertexBuffer")
        };

        _applyBounds = bounds =>
        {
            const float packed32Scale = 2.0f / 1024;
            const float packed64Scale = 2.0f / ushort.MaxValue;
            var offset = bounds.Origin - bounds.BoxExtent;
            var scale = Stride switch
            {
                4 => bounds.BoxExtent * packed32Scale,
                8 => bounds.BoxExtent * packed64Scale,
                12 => bounds.BoxExtent,
                _ => throw new ArgumentOutOfRangeException($"Unknown stride {Stride} for FPositionVertexBuffer")
            };

            for (var i = 0; i < NumVertices; i++)
            {
                Verts[i] = Verts[i] * scale + offset;
            }
        };
    }

    private void ReadRocoKingdomWorld(FArchive Ar)
    {
        Stride = Ar.Read<int>();
        NumVertices = Ar.Read<int>();
        Verts = Stride switch
        {
            8 => Ar.ReadBulkArray(() => (FVector) Ar.Read<FVector3SignedShortScale>()),
            12 => Ar.ReadBulkArray<FVector>(),
            _ => throw new ArgumentOutOfRangeException($"Unknown stride {Stride} for FPositionVertexBuffer")
        };

        if (Stride != 8)
            return;

        _applyBounds = bounds =>
        {
            for (var i = 0; i < Verts.Length; i++)
            {
                Verts[i] = Verts[i] * bounds.BoxExtent + bounds.Origin;
            }
        };
    }

    private void ReadFateTrigger(FArchive Ar)
    {
        Stride = Ar.Read<int>();
        NumVertices = Ar.Read<int>();
        var box = Ar.Read<byte>();
        Verts = Ar.ReadBulkArray<FVector>();
        if (box != 0)
        {
            Ar.Position += 24; // Box
            Ar.SkipBulkArrayData();
        }
    }

    private void ReadWorldofJadeDynasty(FArchive Ar)
    {
        Stride = Ar.Read<int>();
        NumVertices = Ar.Read<int>();
        Stride = (int) (Stride ^ 0xdbb1054f);
        NumVertices >>= 9;
        Verts = Ar.ReadBulkArray<FVector>();
    }

    private void ReadGollum(FArchive Ar)
    {
        Stride = Ar.Read<int>();
        NumVertices = Ar.Read<int>();
        Ar.Position += 25;
        Verts = Ar.ReadBulkArray<FVector>();
    }

    private void ReadGearsofWarEDay(FArchive Ar)
    {
        Stride = Ar.Read<int>();
        NumVertices = Ar.Read<int>();
        if (NumVertices == 0)
            return;
        Verts = Ar.ReadBulkArray<FVector>();
    }

    private void ReadLifeIsStrange(FArchive Ar)
    {
        Stride = Ar.Read<int>();
        NumVertices = Ar.Read<int>();
        if (Ar.LicenseeVer >= 18)
            Ar.Position += 8; // int, bool
        if (Ar.LicenseeVer >= 20)
            Ar.Position += 24; // FVector, FVector
        Verts = Ar.ReadBulkArray<FVector>();
    }
    #endregion
}
