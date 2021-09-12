#include "includes/ACLDecompress.h"

// ACL allocator
DLLEXPORT void* nAllocate(size_t size, size_t alignment) { return ACLAllocatorImpl.allocate(size, alignment); }
DLLEXPORT void nDeallocate(void* ptr, size_t size) { ACLAllocatorImpl.deallocate(ptr, size); }

// ACL compressed tracks
DLLEXPORT const char* nCompressedTracks_IsValid(acl::compressed_tracks* tracks, bool checkHash) { return tracks->is_valid(checkHash).c_str(); }

DLLEXPORT void nReadACLData(const acl::compressed_tracks& tracks, FVector* outPosKeys, FQuat* outRotKeys, FVector* outScaleKeys)
{
    uint32_t numSamples = tracks.get_num_samples_per_track();
    float sampleRate = tracks.get_sample_rate();
    float duration = tracks.get_finite_duration();

    DecompContextDefault context;
    context.initialize(tracks);

    FCUE4ParseOutputWriter writer(outPosKeys, outRotKeys, outScaleKeys, numSamples);
    for (uint32_t sampleIndex = 0; sampleIndex < numSamples; ++sampleIndex)
    {
        const float sample_time = rtm::scalar_min(float(sampleIndex) / sampleRate, duration);
        context.seek(sample_time, acl::sample_rounding_policy::nearest);
        writer.SampleIndex = sampleIndex;
        context.decompress_tracks(writer);
    }
}