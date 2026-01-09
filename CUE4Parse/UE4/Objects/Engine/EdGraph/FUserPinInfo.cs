using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Objects.Engine.EdGraph;

[JsonConverter(typeof(StringEnumConverter))]
public enum EEdGraphPinDirection : byte
{
    EGPD_Input,
    EGPD_Output,
    EGPD_MAX,
}

public class FUserPinInfo
{
    public FName PinName;
    public FEdGraphPinType? PinType;
    public EEdGraphPinDirection DesiredPinDirection;
    public string PinDefaultValue;

    public FUserPinInfo(FAssetArchive Ar)
    {
        if (FFrameworkObjectVersion.Get(Ar) >= FFrameworkObjectVersion.Type.PinsStoreFName)
        {
            PinName = Ar.ReadFName();
        }
        else
        {
            PinName = Ar.ReadFString();
        }

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.SERIALIZE_PINTYPE_CONST)
        {
            PinType = new FEdGraphPinType(Ar);
            DesiredPinDirection = Ar.Read<EEdGraphPinDirection>();
        }
        else
        {
            var bIsArray = Ar.ReadBoolean();
            var bIsReference = Ar.ReadBoolean();
            var PinCategoryStr = Ar.ReadFString();
            var PinSubCategoryStr = Ar.ReadFString();

            if (Ar.Game is >= EGame.GAME_UE5_0 && PinCategoryStr is "double" or "float")
            {
                PinCategoryStr = "real";
                PinSubCategoryStr = "double";
            }
            var PinSubCategoryObject = new FPackageIndex(Ar);
            PinType = new FEdGraphPinType
            {
                ContainerType = bIsArray ? EPinContainerType.Array : EPinContainerType.None,
                bIsReference = bIsReference,
                PinCategory = PinCategoryStr,
                PinSubCategory = PinSubCategoryStr,
                PinSubCategoryObject = PinSubCategoryObject
            };
        }

        PinDefaultValue = Ar.ReadFString();
    }
}
