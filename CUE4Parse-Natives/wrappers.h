#include <stdio.h>
#include "acl/core/compressed_tracks.h"
#include "acl/core/error_result.h"
#include "acl/decompression/decompress.h"

using namespace acl;

#define DLLEXPORT extern "C" __declspec(dllexport)

DLLEXPORT const char* nCompressedTracks_IsValid(compressed_tracks* tracks, bool checkHash) { return tracks->is_valid(checkHash).c_str(); }

using DecompContextDefault = decompression_context<decompression_settings>;

DLLEXPORT DecompContextDefault* nDecompContextDefault_Create() { return new DecompContextDefault(); }
DLLEXPORT const compressed_tracks* nDecompContextDefault_GetCompressedTracks(DecompContextDefault* context) { return context->get_compressed_tracks(); }
DLLEXPORT bool nDecompContextDefault_Initialize(DecompContextDefault* context, compressed_tracks& tracks) { return context->initialize(tracks); }
DLLEXPORT void nDecompContextDefault_Seek(DecompContextDefault* context, int64_t sampleTime, sample_rounding_policy roundingPolicy) { context->seek(sampleTime, roundingPolicy); }

struct FVector
{
    float X;
    float Y;
    float Z;

    FVector() : X(0), Y(0), Z(0)
    {
    }

    FVector(float x, float y, float z)
        : X(x), Y(y), Z(z)
    {
    }

    FVector operator-(FVector v)
    {
        return FVector(X - v.X, Y - v.Y, Z - v.Z);
    }

    FVector operator+(FVector v)
    {
        return FVector(X + v.X, Y + v.Y, Z + v.Z);
    }

    float Distance(FVector v)
    {
        return ((X - v.X) * (X - v.X) +
            (Y - v.Y) * (Y - v.Y) +
            (Z - v.Z) * (Z - v.Z));
    }
};

struct FRotator
{
    float Pitch;
    float Yaw;
    float Roll;

    FRotator() : Pitch(0), Yaw(0), Roll(0)
    {
    }

    FRotator(float pitch, float yaw, float roll)
        : Pitch(pitch), Yaw(yaw), Roll(roll)
    {
    }
};

struct FQuat
{
    float X, Y, Z, W;

    FQuat() : X(0), Y(0), Z(0), W(0)
    {
    }

    FQuat(float x, float y, float z, float w)
        : X(x), Y(y), Z(z), W(w)
    {
    }
};

struct FTransform
{
    FQuat Rotation;
    FVector Translation;
    FVector Scale3D;
};

struct FACLTransform final : public FTransform
{
    __forceinline void RTM_SIMD_CALL SetRotationRaw(rtm::quatf_arg0 Rotation_)
    {
#if PLATFORM_ENABLE_VECTORINTRINSICS
        Rotation = Rotation_;
#else
        rtm::quat_store(Rotation_, &Rotation.X);
#endif
    }

    __forceinline void RTM_SIMD_CALL SetTranslationRaw(rtm::vector4f_arg0 Translation_)
    {
#if PLATFORM_ENABLE_VECTORINTRINSICS
        Translation = VectorSet_W0(Translation_);
#else
        rtm::vector_store3(Translation_, &Translation.X);
#endif
    }

    __forceinline void RTM_SIMD_CALL SetScale3DRaw(rtm::vector4f_arg0 Scale_)
    {
#if PLATFORM_ENABLE_VECTORINTRINSICS
        Scale3D = VectorSet_W0(Scale_);
#else
        rtm::vector_store3(Scale_, &Scale3D.X);
#endif
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

DLLEXPORT UE4OutputTrackWriter* nCreateTrackWriter(FTransform& Atom) { return new UE4OutputTrackWriter(Atom); }

DLLEXPORT void nDecompContextDefault_DecompressTrack(DecompContextDefault* context, uint32_t trackIndex, UE4OutputTrackWriter& writer) { context->decompress_track(trackIndex, writer); }

DLLEXPORT void* nAlignedMalloc(size_t size, size_t alignment) { return _aligned_malloc(size, alignment); }
DLLEXPORT void nAlignedFree(void* ptr) { _aligned_free(ptr); }