using System.Text;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Verse
{
    public enum EVerseDigestVariant : byte
    {
        PublicOnly = 0,
        PublicAndEpicInternal = 1,
        EVerseDigestVariant_MAX = 2
    }

    /// <summary>
    /// Verse Visual Process Language
    /// </summary>
    public class UVerseDigest : UObject
    {
        public string ProjectName { get; private set; }
        public EVerseDigestVariant Variant { get; private set; }
        public string ReadableCode { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            ProjectName = GetOrDefault<string>(nameof(ProjectName));
            Variant = GetOrDefault<EVerseDigestVariant>(nameof(Variant));

            ReadableCode = Encoding.UTF8.GetString(GetOrDefault<byte[]>("DigestCode"));
        }
    }
}
