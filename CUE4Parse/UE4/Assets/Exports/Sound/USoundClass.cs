using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Sound
{
    public struct NodeEditorData
    {
        public int X;
        public int Y;
    }

    public class USoundClass : UObject
    {
        public Dictionary<FPackageIndex?, NodeEditorData>? EditorData;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            if (Ar.Ver >= EUnrealEngineObjectUE3Version.SOUND_CLASS_SERIALISATION_UPDATE)
            {
                EditorData = new Dictionary<FPackageIndex, NodeEditorData>();
                int Count = Ar.Read<int>();

                for (int i = 0; i < Count; i++)
                {
                    var key = new FPackageIndex(Ar); // Sometimes can be null, ReadMap can't be used.
                    var value = Ar.Read<NodeEditorData>();

                    if (key != null)
                        EditorData[key] = value;
                }
            }
        }
    }
}
