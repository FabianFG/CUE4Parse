#pragma once
#include "ACLFramework.h"

struct FCUE4ParseOutputWriter final : public acl::track_writer
{
    FVector* Translations;
    FQuat* Rotations;
    FVector* Scales;
    uint32_t NumSamples;
    uint32_t SampleIndex;

    FCUE4ParseOutputWriter(FVector* inTranslations, FQuat* inRotations, FVector* inScales, uint32_t inNumSamples)
        : Translations(inTranslations)
        , Rotations(inRotations)
        , Scales(inScales)
        , NumSamples(inNumSamples)
    {}

    RTM_FORCE_INLINE void RTM_SIMD_CALL write_rotation(uint32_t trackIndex, rtm::quatf_arg0 rotation)
    {
        rtm::quat_store(rotation, &Rotations[trackIndex * NumSamples + SampleIndex].X);
    }

    RTM_FORCE_INLINE void RTM_SIMD_CALL write_translation(uint32_t trackIndex, rtm::vector4f_arg0 translation)
    {
        rtm::vector_store3(translation, &Translations[trackIndex * NumSamples + SampleIndex].X);
    }

    RTM_FORCE_INLINE void RTM_SIMD_CALL write_scale(uint32_t trackIndex, rtm::vector4f_arg0 scale)
    {
        rtm::vector_store3(scale, &Scales[trackIndex * NumSamples + SampleIndex].X);
    }
};