using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Sound;

public struct FSoundClassEditorData
{
    public int X;
    public int Y;
}

public class USoundClass : UObject
{
    public KeyValuePair<FPackageIndex, FSoundClassEditorData>[]? EditorData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (Ar.Ver >= EUnrealEngineObjectUE3Version.SOUND_CLASS_SERIALISATION_UPDATE)
        {
            EditorData = Ar.ReadArray<KeyValuePair<FPackageIndex, FSoundClassEditorData>>(() => new(new FPackageIndex(Ar), Ar.Read<FSoundClassEditorData>()));
        }
    }
}
