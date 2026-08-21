using CUE4Parse.GameTypes.Borderlands4.Assets.Objects;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.Borderlands4.Wwise;

public static class GbxAudioUtil
{
    private static readonly Dictionary<FName, bool> _gbxAudioEvents = [];
    private static readonly object _lock = new();

    public static void TryRegisterEvent(string typeIdentifier, FStructFallback? fallback, TypeMappings? mappings)
    {
        if (fallback is null)
            return;

        FGbxAudioEvent gbxAudioEvent;
        if (MatchesHandler(typeIdentifier, "GbxAudioBodyAction_PostEvent", mappings))
        {
            gbxAudioEvent = new FGbxAudioBodyAction_PostEvent(fallback).ActivationSound;
        }
        else if (MatchesHandler(typeIdentifier, "GbxAudioBodyAction_ManagedLoop", mappings))
        {
            gbxAudioEvent = new FGbxAudioBodyAction_ManagedLoop(fallback).LoopStartEvent;
        }
        else if (MatchesHandler(typeIdentifier, "GbxAudioNodeAspectSettings_PostEvent", mappings))
        {
            gbxAudioEvent = new FGbxAudioNodeAspectSettings_PostEvent(fallback).AudioEvent;
        }
        else return;

        var bUseSoundTag = gbxAudioEvent.bUseSoundTag;
        AddEvent(bUseSoundTag ? gbxAudioEvent.SoundTag.TagName : gbxAudioEvent.WwiseEvent.Name, bUseSoundTag);
    }

    private static bool MatchesHandler(string identifier, string legacyName, TypeMappings? mappings) =>
        mappings?.MatchesResolvedTypeIdentifier(identifier, legacyName) ??
        identifier.Equals(legacyName, StringComparison.OrdinalIgnoreCase);

    private static void AddEvent(FName eventName, bool useSoundTag)
    {
        if (eventName.IsNone)
            return;

        lock (_lock)
        {
            _gbxAudioEvents[eventName] = useSoundTag;
        }
    }

    public static Dictionary<FName, bool> GetAndClearEvents()
    {
        lock (_lock)
        {
            var snapshot = new Dictionary<FName, bool>(_gbxAudioEvents);
            _gbxAudioEvents.Clear();
            return snapshot;
        }
    }
}
