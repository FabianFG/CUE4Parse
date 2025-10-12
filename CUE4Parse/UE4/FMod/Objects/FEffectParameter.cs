using System.IO;

namespace CUE4Parse.UE4.FMod.Objects;

public readonly struct FEffectParameter
{
    public readonly int Type;
    public readonly float FloatValue;
    public readonly byte[]? Buffer;

    public FEffectParameter(BinaryReader Ar)
    {
        int paramType = Ar.ReadInt32();
        if (paramType < 0 || paramType > 3) throw new InvalidDataException($"Invalid parameter type: {paramType}");

        Type = paramType;

        switch (Type)
        {
            case 0:
            case 1:
                FloatValue = Ar.ReadSingle();
                break;
            case 2:
                FloatValue = Ar.ReadBoolean() ? 1f : 0f;
                break;
            case 3:
                uint length = FModReader.ReadX16(Ar);
                if (length > 0)
                {
                    Buffer = Ar.ReadBytes((int)length);
                }

                FloatValue = 0;
                break;
            default:
                throw new InvalidDataException($"Unknown parameter type: {paramType}");
        }
    }
}
