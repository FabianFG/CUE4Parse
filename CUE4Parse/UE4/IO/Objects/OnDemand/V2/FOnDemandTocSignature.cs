using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects.OnDemand.V2;

public class FOnDemandTocSignature(FArchive Ar)
{
    private readonly string _signature = Ar.ReadFUtf8String(ExpectedSignature.Length);
    private const string ExpectedSignature = "UE ON-DEMAND TOC";

    public bool IsValid() => _signature.Equals(ExpectedSignature, StringComparison.Ordinal);
    public override string ToString() => $"Signature: {_signature} IsValid: {IsValid()} ";
}
