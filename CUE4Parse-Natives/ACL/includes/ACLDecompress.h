#pragma once
#include "ACLFramework.h"

inline rtm::vector4f RTM_SIMD_CALL UEVector3ToACL(const FVector& Input) { return rtm::vector_set(Input.X, Input.Y, Input.Z); }
inline rtm::quatf RTM_SIMD_CALL UEQuatToACL(const FQuat& Input) { return rtm::quat_set(Input.X, Input.Y, Input.Z, Input.W); }

template<bool bUseBindPose>
struct FCUE4ParseOutputWriter final : public acl::track_writer
{
    FTransform* RefPoses;
    FTrackToSkeletonMap* TrackToBoneMapping;

    FACLTransform* Atoms;
    uint32_t NumSamples;
    uint32_t SampleIndex;

    FCUE4ParseOutputWriter(FTransform* inRefPoses, FTrackToSkeletonMap* inTrackToSkeletonMap, FTransform* inAtoms, uint32_t inNumSamples)
        : RefPoses(inRefPoses)
        , TrackToBoneMapping(inTrackToSkeletonMap)
        , Atoms(static_cast<FACLTransform*>(inAtoms))
        , NumSamples(inNumSamples)
    {}

    static constexpr acl::default_sub_track_mode get_default_rotation_mode() { return bUseBindPose ? acl::default_sub_track_mode::variable : acl::default_sub_track_mode::constant; }
    static constexpr acl::default_sub_track_mode get_default_translation_mode() { return bUseBindPose ? acl::default_sub_track_mode::variable : acl::default_sub_track_mode::constant; }
    static constexpr acl::default_sub_track_mode get_default_scale_mode() { return bUseBindPose ? acl::default_sub_track_mode::variable : acl::default_sub_track_mode::legacy; }

    RTM_FORCE_INLINE rtm::quatf RTM_SIMD_CALL get_variable_default_rotation(uint32_t trackIndex) const
    {
        return UEQuatToACL(RefPoses[TrackToBoneMapping[trackIndex].BoneTreeIndex].Rotation);
    }

    RTM_FORCE_INLINE rtm::vector4f RTM_SIMD_CALL get_variable_default_translation(uint32_t trackIndex) const
    {
        return UEVector3ToACL(RefPoses[TrackToBoneMapping[trackIndex].BoneTreeIndex].Translation);
    }

    RTM_FORCE_INLINE rtm::vector4f RTM_SIMD_CALL get_variable_default_scale(uint32_t trackIndex) const
    {
        return UEVector3ToACL(RefPoses[TrackToBoneMapping[trackIndex].BoneTreeIndex].Scale3D);
    }

    RTM_FORCE_INLINE void RTM_SIMD_CALL write_rotation(uint32_t trackIndex, rtm::quatf_arg0 rotation)
    {
        Atoms[trackIndex * NumSamples + SampleIndex].SetRotationRaw(rotation);
    }

    RTM_FORCE_INLINE void RTM_SIMD_CALL write_translation(uint32_t trackIndex, rtm::vector4f_arg0 translation)
    {
        Atoms[trackIndex * NumSamples + SampleIndex].SetTranslationRaw(translation);
    }

    RTM_FORCE_INLINE void RTM_SIMD_CALL write_scale(uint32_t trackIndex, rtm::vector4f_arg0 scale)
    {
        Atoms[trackIndex * NumSamples + SampleIndex].SetScale3DRaw(scale);
    }
};

struct FCUE4ParseCurveWriter final : public acl::track_writer
{
	float* Floats;
    uint32_t NumSamples;
    uint32_t SampleIndex;

    FCUE4ParseCurveWriter(float* inFloats, uint32_t inNumSamples)
        : Floats(inFloats)
        , NumSamples(inNumSamples)
    {}

	RTM_FORCE_INLINE void RTM_SIMD_CALL write_float1(uint32_t trackIndex, rtm::scalarf_arg0 floatValue)
    {
        Floats[trackIndex * NumSamples + SampleIndex] = rtm::scalar_cast(floatValue);
    }
};
