using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Verse;

public class UVerseDigestEditorOptionalData : UObject
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        // The CDO is outered to the script package, not a digest, and has no payload to carry.
        if (Flags.HasFlag(EObjectFlags.RF_ClassDefaultObject) || Outer is null ||
            !Outer.TryLoad<UVerseDigest>(out var outer) || outer.Flags.HasFlag(EObjectFlags.RF_ClassDefaultObject))
        {
             return;
        }

        outer.LoadDigestPayload(Ar);
    }
}
