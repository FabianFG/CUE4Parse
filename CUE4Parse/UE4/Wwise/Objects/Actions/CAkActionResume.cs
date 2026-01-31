using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class CAkActionResume
{
    public readonly CAkActionParams ActionParams;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EResumeOptions ResumeOptions;
    public readonly CAkActionExcept ExceptParams;

    // CAkActionResume::SetActionActiveParams
    public CAkActionResume(FArchive Ar)
    {
        ActionParams = new CAkActionParams(Ar);

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

        ExceptParams = new CAkActionExcept(Ar);
    }
}
