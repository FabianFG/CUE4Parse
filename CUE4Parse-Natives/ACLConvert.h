#pragma once
#include <acl/compression/convert.h>
#include <acl/compression/track_array.h>
#include "Framework.h"

DLLEXPORT const char* nConvertTrackList(const acl::compressed_tracks& tracks, acl::track_array_qvvf& outTracks) { return acl::convert_track_list(ACLAllocatorImpl, tracks, outTracks).c_str(); }

// The out params must be allocated with size UAnimSequence::NumFrames
DLLEXPORT const char* nConvertACLTrack(const acl::track_array_qvvf& tracks, uint32_t trackIndex, FVector* outPosKeys, FQuat* outRotKeys, FVector* outScaleKeys)
{
    const uint32_t numSamples = tracks.get_num_samples_per_track();
    if (numSamples == 0)
    {
        return "Clip has no samples";
    }

    if (!outPosKeys) outPosKeys = (FVector*) malloc(numSamples * sizeof(FVector));
    if (!outRotKeys) outRotKeys = (FQuat*) malloc(numSamples * sizeof(FQuat));
    if (!outScaleKeys) outScaleKeys = (FVector*) malloc(numSamples * sizeof(FVector));
    const uint32_t numTracks = tracks.get_num_tracks();
    for (uint32_t trackIndex = 0; trackIndex < numTracks; ++trackIndex)
    {
        const acl::track_qvvf& track = tracks[trackIndex];

        for (uint32_t sampleIndex = 0; sampleIndex < numSamples; ++sampleIndex)
        {
            const FQuat rotation = QuatCast(rtm::quat_normalize(track[sampleIndex].rotation));
            outRotKeys[sampleIndex] = rotation;
        }

        for (uint32_t sampleIndex = 0; sampleIndex < numSamples; ++sampleIndex)
        {
            const FVector translation = VectorCast(track[sampleIndex].translation);
            outPosKeys[sampleIndex] = translation;
        }

        for (uint32_t sampleIndex = 0; sampleIndex < numSamples; ++sampleIndex)
        {
            const FVector scale = VectorCast(track[sampleIndex].scale);
            outScaleKeys[sampleIndex] = scale;
        }
    }
}