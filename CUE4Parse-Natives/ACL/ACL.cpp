#include "includes/ACLDecompress.h"

// Forward declaration
template <bool bUseBindPose>
void ProcessTracks(const acl::compressed_tracks& tracks, FTransform* inRefPoses, FTrackToSkeletonMap* inTrackToSkeletonMap, FTransform* outAtom);

// ACL allocator
DLLEXPORT void* nAllocate(size_t size, size_t alignment) { return ACLAllocatorImpl.allocate(size, alignment); }
DLLEXPORT void nDeallocate(void* ptr, size_t size) { ACLAllocatorImpl.deallocate(ptr, size); }

// ACL compressed tracks
DLLEXPORT const char* nCompressedTracks_IsValid(acl::compressed_tracks* tracks, bool checkHash) { return tracks->is_valid(checkHash).c_str(); }
DLLEXPORT void nTracksHeader_SetDefaultScale(acl::acl_impl::tracks_header* header, float defaultScale) { header->set_default_scale(defaultScale); }

DLLEXPORT void nReadACLData(const acl::compressed_tracks& tracks, FTransform* inRefPoses, FTrackToSkeletonMap* inTrackToSkeletonMap, FTransform* outAtom)
{
    if (tracks.get_default_scale() != 0)
    {
        ProcessTracks<true>(tracks, inRefPoses, inTrackToSkeletonMap, outAtom);
    }
    else
    {
        ProcessTracks<false>(tracks, inRefPoses, inTrackToSkeletonMap, outAtom);
    }
}

DLLEXPORT void nReadCurveACLData(const acl::compressed_tracks& tracks, float* outFloatKeys)
{
    uint32_t numSamples = tracks.get_num_samples_per_track();
    float sampleRate = tracks.get_sample_rate();
    float duration = tracks.get_finite_duration();

    DecompContextDefault context;
    context.initialize(tracks);

    FCUE4ParseCurveWriter writer(outFloatKeys, numSamples);
    for (uint32_t sampleIndex = 0; sampleIndex < numSamples; ++sampleIndex)
    {
        const float sample_time = rtm::scalar_min(float(sampleIndex) / sampleRate, duration);
        context.seek(sample_time, acl::sample_rounding_policy::nearest);
        writer.SampleIndex = sampleIndex;
        context.decompress_tracks(writer);
    }
}

template <bool bUseBindPose>
void ProcessTracks(const acl::compressed_tracks& tracks, FTransform* inRefPoses, FTrackToSkeletonMap* inTrackToSkeletonMap, FTransform* outAtom)
{
    uint32_t numSamples = tracks.get_num_samples_per_track();
    float sampleRate = tracks.get_sample_rate();
    float duration = tracks.get_finite_duration();

    DecompContextDefault context;
    context.initialize(tracks);

    FCUE4ParseOutputWriter<bUseBindPose> writer(inRefPoses, inTrackToSkeletonMap, outAtom, numSamples);
    for (uint32_t sampleIndex = 0; sampleIndex < numSamples; ++sampleIndex)
    {
        const float sample_time = rtm::scalar_min(float(sampleIndex) / sampleRate, duration);
        context.seek(sample_time, acl::sample_rounding_policy::nearest);
        writer.SampleIndex = sampleIndex;
        context.decompress_tracks(writer);
    }
}
