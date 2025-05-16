using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class AkActionResume
{
    public ActionParams ActionParams { get; private set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public EResumeOptions ResumeOptions { get; private set; }
    public ExceptParams ExceptParams { get; private set; }

    public AkActionResume(FArchive Ar)
    {
        ActionParams = new ActionParams(Ar);

        if (WwiseVersions.WwiseVersion <= 56)
        {
            Ar.Read<uint>(); // IsMaster
        }
        else if (WwiseVersions.WwiseVersion <= 62)
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
