using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.Engine.Midi;

public class FTypedParameter : IUStruct
{
    public byte Version;
    public FVariant Value;
    public FTypedParameter(FAssetArchive Ar)
    {
        Version = Ar.Read<byte>();
        Value = new FVariant(Ar);
    }
}