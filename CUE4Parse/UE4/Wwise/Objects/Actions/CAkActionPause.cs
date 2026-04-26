using CUE4Parse.UE4.Wwise.Enums.Flags;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class CAkActionPause
{
    public readonly CAkActionParams ActionParams;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EPauseOptions PauseOptions;
    public readonly CAkActionExcept ExceptParams;

    // CAkActionPause::SetActionSpecificParams
    public CAkActionPause(FWwiseArchive Ar)
    {
        ActionParams = new CAkActionParams(Ar);

        if (Ar.Version <= 56)
        {
            Ar.Read<uint>(); // IsMaster
        }
        else if (Ar.Version <= 62)
        {
            Ar.Read<byte>(); // IsMaster
        }
        else
        {
            PauseOptions = Ar.Read<EPauseOptions>();
        }

        ExceptParams = new CAkActionExcept(Ar);
    }
}
