using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.GameTypes.Wave.Assets.Exports;

public class UWavesArenaBGMeshBase : UComponent
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        CustomGameData = Ar.ReadFName();
    }
}

public class UWavesArenaBGMeshComponentUnlit : UWavesArenaBGMeshBase;
public class UWavesArenaBGMeshComponent : UWavesArenaBGMeshBase;
