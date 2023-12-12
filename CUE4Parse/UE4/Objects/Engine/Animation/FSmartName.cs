using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Writers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Engine.Animation;

//[JsonConverter(typeof(FSmartNameConverter))] For consistency with the property serialization structure
public readonly struct FSmartName : IUStruct, ISerializable
{
    public readonly FName DisplayName;
    private readonly ushort TempUID;
    private readonly FGuid TempGUID;

    public FSmartName(FArchive Ar)
    {
        DisplayName = Ar.ReadFName();
        if (FAnimPhysObjectVersion.Get(Ar) < FAnimPhysObjectVersion.Type.RemoveUIDFromSmartNameSerialize)
        {
            TempUID = Ar.Read<ushort>(); // TempUID
        }

        // only save if it's editor build and not cooking
        if (FAnimPhysObjectVersion.Get(Ar) < FAnimPhysObjectVersion.Type.SmartNameRefactorForDeterministicCooking)
        {
            TempGUID = Ar.Read<FGuid>(); // TempGUID
        }
    }

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Serialize(DisplayName);

        // TODO: Add versioning
        // if (FAnimPhysObjectVersion.Get(Ar) < FAnimPhysObjectVersion.Type.RemoveUIDFromSmartNameSerialize)
        // {
        //     Ar.Write(TempUID); // TempUID
        // }

        // if (FAnimPhysObjectVersion.Get(Ar) < FAnimPhysObjectVersion.Type.SmartNameRefactorForDeterministicCooking)
        // {
        //     Ar.Serialize(TempGUID);
        // }
    }

    public FSmartName(FStructFallback data)
    {
        DisplayName = data.GetOrDefault<FName>(nameof(DisplayName));
    }
}