using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.FastGeoStreaming;

public class FFastGeoComponent
{
    public readonly int ComponentIndex;
    public readonly FTransform LocalTransform;
    public readonly FTransform WorldTransform;
    public EDetailMode DetailMode { get; protected init; }

    public FFastGeoComponent(FArchive Ar)
    {
        ComponentIndex = Ar.Read<int>();
        if (Ar.Game is GAME_GearsofWarEDay) Ar.Position += 8;
        LocalTransform = new FTransform(Ar);
        WorldTransform = new FTransform(Ar);
        DetailMode = Ar.Game is >= GAME_UE5_8 or GAME_GearsofWarEDay ? Ar.Read<EDetailMode>() : EDetailMode.Low;
    }
}
