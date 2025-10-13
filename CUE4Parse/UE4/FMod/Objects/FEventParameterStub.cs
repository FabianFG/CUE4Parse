using System.IO;

namespace CUE4Parse.UE4.FMod.Objects;

public readonly struct FEventParameterStub
{
    public readonly uint StubIndex;
    public readonly FModGuid ParameterGuid;
    public readonly float InitialValue;

    public FEventParameterStub(BinaryReader Ar)
    {
        StubIndex = Ar.ReadUInt32();
        ParameterGuid = new FModGuid(Ar);
        InitialValue = Ar.ReadSingle();
    }
}
