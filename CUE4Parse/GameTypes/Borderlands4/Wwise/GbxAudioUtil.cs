using System.Collections.Generic;
using CUE4Parse.GameTypes.Borderlands4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.Borderlands4.Wwise;

public static class GbxAudioUtil
{
    private static readonly Dictionary<FName, bool> _gbxAudioEvents = [];
    private static readonly object _lock = new();

    public static void TryRegisterEvent(string typeName, FStructFallback? fallback)
    {
        if (fallback is null)
            return;

        FGbxAudioEvent gbxAudioEvent;
        switch (typeName)
        {
            case "GbxAudioBodyAction_PostEvent":
                gbxAudioEvent = new FGbxAudioBodyAction_PostEvent(fallback).ActivationSound;
                break;
            case "GbxAudioBodyAction_ManagedLoop":
                gbxAudioEvent = new FGbxAudioBodyAction_ManagedLoop(fallback).LoopStartEvent;
                break;
            case "GbxAudioNodeAspectSettings_PostEvent":
                gbxAudioEvent = new FGbxAudioNodeAspectSettings_PostEvent(fallback).AudioEvent;
                break;
            default:
                return;
        }

        var bUseSoundTag = gbxAudioEvent.bUseSoundTag;
        AddEvent(bUseSoundTag ? gbxAudioEvent.SoundTag.TagName : gbxAudioEvent.WwiseEvent.Name, bUseSoundTag);
    }

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
