#pragma once
#include <acl/decompression/decompress.h>
#include "Structs.h"
#include "Framework.h"

using DecompContextDefault = acl::decompression_context<acl::decompression_settings>;

DLLEXPORT DecompContextDefault* nDecompContextDefault_Create() { return new DecompContextDefault(); }
DLLEXPORT const acl::compressed_tracks* nDecompContextDefault_GetCompressedTracks(DecompContextDefault* context) { return context->get_compressed_tracks(); }
DLLEXPORT bool nDecompContextDefault_Initialize(DecompContextDefault* context, acl::compressed_tracks& tracks) { return context->initialize(tracks); }
DLLEXPORT void nDecompContextDefault_Seek(DecompContextDefault* context, int64_t sampleTime, acl::sample_rounding_policy roundingPolicy) { context->seek(sampleTime, roundingPolicy); }

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

DLLEXPORT FUE4OutputWriter* nCreateOutputWriter(FTransform* atoms, const FAtomIndices* trackToAtomsMap) { return new FUE4OutputWriter(atoms, trackToAtomsMap); }
DLLEXPORT UE4OutputTrackWriter* nCreateOutputTrackWriter(FTransform& atom) { return new UE4OutputTrackWriter(atom); }

DLLEXPORT void nDecompContextDefault_DecompressTracks(DecompContextDefault* context, FUE4OutputWriter& writer) { context->decompress_tracks(writer); }
DLLEXPORT void nDecompContextDefault_DecompressTrack(DecompContextDefault* context, uint32_t trackIndex, UE4OutputTrackWriter& writer) { context->decompress_track(trackIndex, writer); }