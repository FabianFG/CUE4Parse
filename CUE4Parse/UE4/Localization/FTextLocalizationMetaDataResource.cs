using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Localization
{
    [JsonConverter(typeof(FTextLocalizationMetaDataResourceConverter))]
    public class FTextLocalizationMetaDataResource
    {
        private readonly FGuid _locMetaMagic = new (0xA14CEE4Fu, 0x83554868u, 0xBD464C6Cu, 0x7C50DA70u);
        public readonly string NativeCulture;
        public readonly string NativeLocRes;
        public readonly string[]? CompiledCultures;

        public FTextLocalizationMetaDataResource(FArchive Ar)
        {
            var versionNumber = ELocMetaVersion.Initial;
            var locResMagic = Ar.Read<FGuid>();
            if (locResMagic == _locMetaMagic)
            {
                versionNumber = Ar.Read<ELocMetaVersion>();
            }
            else
            {
                Ar.Position = 0;
                Log.Warning($"LocMeta '{Ar.Name}' failed the magic number check!");
            }

            // Is this LocRes file too new to load?
            if (versionNumber > ELocMetaVersion.Latest)
            {
                throw new ParserException(Ar, $"LocMeta '{Ar.Name}' is too new to be loaded (File Version: {versionNumber:D}, Loader Version: {ELocResVersion.Latest:D})");
            }

            NativeCulture = Ar.ReadFString();
            NativeLocRes = Ar.ReadFString();

            if (versionNumber >= ELocMetaVersion.AddedCompiledCultures)
            {
                CompiledCultures = Ar.ReadArray(Ar.ReadFString);
            }
            else
            {
                CompiledCultures = null;
            }
        }
    }
}
