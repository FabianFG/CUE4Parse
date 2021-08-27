#pragma once
#include <acl/core/ansi_allocator.h>
#include <acl/core/compressed_tracks.h>
#include "Structs.h"

#define DLLEXPORT extern "C" __declspec(dllexport)

acl::ansi_allocator ACLAllocatorImpl;

DLLEXPORT void* nAllocate(size_t size, size_t alignment) { return ACLAllocatorImpl.allocate(size, alignment); }
DLLEXPORT void nDeallocate(void* ptr, size_t size) { ACLAllocatorImpl.deallocate(ptr, size); }

/** RTM <-> UE4 conversion utilities */
inline rtm::vector4f RTM_SIMD_CALL VectorCast(const FVector& Input) { return rtm::vector_set(Input.X, Input.Y, Input.Z); }
inline FVector RTM_SIMD_CALL VectorCast(rtm::vector4f_arg0 Input) { return FVector(rtm::vector_get_x(Input), rtm::vector_get_y(Input), rtm::vector_get_z(Input)); }
inline rtm::quatf RTM_SIMD_CALL QuatCast(const FQuat& Input) { return rtm::quat_set(Input.X, Input.Y, Input.Z, Input.W); }
inline FQuat RTM_SIMD_CALL QuatCast(rtm::quatf_arg0 Input) { return FQuat(rtm::quat_get_x(Input), rtm::quat_get_y(Input), rtm::quat_get_z(Input), rtm::quat_get_w(Input)); }
// inline rtm::qvvf RTM_SIMD_CALL TransformCast(const FTransform& Input) { return rtm::qvv_set(QuatCast(Input.Rotation), VectorCast(Input.Translation), VectorCast(Input.Scale3D)); }
// inline FTransform RTM_SIMD_CALL TransformCast(rtm::qvvf_arg0 Input) { return FTransform(QuatCast(Input.rotation), VectorCast(Input.translation), VectorCast(Input.scale)); }

DLLEXPORT const char* nCompressedTracks_IsValid(acl::compressed_tracks* tracks, bool checkHash) { return tracks->is_valid(checkHash).c_str(); }