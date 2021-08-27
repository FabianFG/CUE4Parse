#pragma once
#include "Framework.h"

struct FACLTransform final : public FTransform
{
    __forceinline void RTM_SIMD_CALL SetRotationRaw(rtm::quatf_arg0 Rotation_)
    {
        rtm::quat_store(Rotation_, &Rotation.X);
    }

    __forceinline void RTM_SIMD_CALL SetTranslationRaw(rtm::vector4f_arg0 Translation_)
    {
        rtm::vector_store3(Translation_, &Translation.X);
    }

    __forceinline void RTM_SIMD_CALL SetScale3DRaw(rtm::vector4f_arg0 Scale_)
    {
        rtm::vector_store3(Scale_, &Scale3D.X);
    }
};

struct FAtomIndices
{
    uint16_t Rotation;
    uint16_t Translation;
    uint16_t Scale;
};

struct FUE4OutputWriter final : public acl::track_writer
{
    FACLTransform* Atoms;
    const FAtomIndices* TrackToAtomsMap;

    FUE4OutputWriter(FTransform* Atoms_, const FAtomIndices* TrackToAtomsMap_)
        : Atoms(static_cast<FACLTransform*>(Atoms_))
        , TrackToAtomsMap(TrackToAtomsMap_)
    {}

    __forceinline bool skip_track_rotation(uint32_t BoneIndex) const { return TrackToAtomsMap[BoneIndex].Rotation == 0xFFFF; }
    __forceinline bool skip_track_translation(uint32_t BoneIndex) const { return TrackToAtomsMap[BoneIndex].Translation == 0xFFFF; }
    __forceinline bool skip_track_scale(uint32_t BoneIndex) const { return TrackToAtomsMap[BoneIndex].Scale == 0xFFFF; }

    __forceinline void RTM_SIMD_CALL write_rotation(uint32_t BoneIndex, rtm::quatf_arg0 Rotation)
    {
        const uint32_t AtomIndex = TrackToAtomsMap[BoneIndex].Rotation;

        FACLTransform& BoneAtom = Atoms[AtomIndex];
        BoneAtom.SetRotationRaw(Rotation);
    }

    __forceinline void RTM_SIMD_CALL write_translation(uint32_t BoneIndex, rtm::vector4f_arg0 Translation)
    {
        const uint32_t AtomIndex = TrackToAtomsMap[BoneIndex].Translation;

        FACLTransform& BoneAtom = Atoms[AtomIndex];
        BoneAtom.SetTranslationRaw(Translation);
    }

    __forceinline void RTM_SIMD_CALL write_scale(uint32_t BoneIndex, rtm::vector4f_arg0 Scale)
    {
        const uint32_t AtomIndex = TrackToAtomsMap[BoneIndex].Scale;

        FACLTransform& BoneAtom = Atoms[AtomIndex];
        BoneAtom.SetScale3DRaw(Scale);
    }
};

struct UE4OutputTrackWriter final : public acl::track_writer
{
    FACLTransform* Atom;

    UE4OutputTrackWriter(FTransform& Atom_)
        : Atom(static_cast<FACLTransform*>(&Atom_))
    {}

    void RTM_SIMD_CALL write_rotation(uint32_t BoneIndex, rtm::quatf_arg0 Rotation)
    {
        Atom->SetRotationRaw(Rotation);
    }

    void RTM_SIMD_CALL write_translation(uint32_t BoneIndex, rtm::vector4f_arg0 Translation)
    {
        Atom->SetTranslationRaw(Translation);
    }

    void RTM_SIMD_CALL write_scale(uint32_t BoneIndex, rtm::vector4f_arg0 Scale)
    {
        Atom->SetScale3DRaw(Scale);
    }
};