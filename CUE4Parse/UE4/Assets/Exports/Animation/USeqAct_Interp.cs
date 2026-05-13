using System.Collections.Generic;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Animation;

[StructLayout(LayoutKind.Sequential)]
public struct FSavedTransform
{
    public FVector Location;
    public FRotator Rotation;
}

public class USeqAct_Interp : UObject
{
    public Dictionary<FPackageIndex, FSavedTransform>? SavedActorTransforms;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.ADDED_SEQACT_INTERP_SAVEACTORTRANSFORMS)
        {
            SavedActorTransforms = Ar.ReadMap(() => new FPackageIndex(Ar), Ar.Read<FSavedTransform>);
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        
        writer.WritePropertyName(nameof(SavedActorTransforms));
        serializer.Serialize(writer, SavedActorTransforms);
    }
}
