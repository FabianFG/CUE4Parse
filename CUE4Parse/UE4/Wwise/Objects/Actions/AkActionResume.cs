using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class AkActionResume
{
    public readonly ActionParams ActionParams;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EResumeOptions ResumeOptions;
    public readonly ExceptParams ExceptParams;

    public AkActionResume(FArchive Ar)
    {
        ActionParams = new ActionParams(Ar);

        if (WwiseVersions.Version <= 56)
        {
            Ar.Read<uint>(); // IsMaster
        }
        else if (WwiseVersions.Version <= 62)
        {
            Ar.Read<byte>(); // IsMaster
        }
        else
        {
            ResumeOptions = Ar.Read<EResumeOptions>();
        }

        ExceptParams = new ExceptParams(Ar);
    }
}
