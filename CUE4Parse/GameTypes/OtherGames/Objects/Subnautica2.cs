using System.Runtime.InteropServices;
using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.GameTypes.OtherGames.Objects;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 3)]
public struct FUWEWorldPopSpatialCell
{
    public bool IsEmpty;
    public bool IsPlayableSpace;
    public bool IsLandscapeCell;
}

public class FUWEWorldPopSpatialLayer(FAssetArchive Ar) : IUStruct
{
    public Dictionary<long, FUWEWorldPopSpatialCell> CellMap = Ar.ReadMap(Ar.Read<long>, Ar.Read<FUWEWorldPopSpatialCell>);
}

public class AMercunaNavDataChunk : AActor
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        if (Ar.Game is EGame.GAME_Subnautica2)
        {
            Ar.Position += 8;
            Ar.Position += Ar.Read<int>();
        }

        base.Deserialize(Ar, validPos);
    }
}
