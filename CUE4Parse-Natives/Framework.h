#pragma once
#include <acl/core/ansi_allocator.h>
#include <acl/core/compressed_tracks.h>
#include <acl/decompression/decompress.h>
#include <acl/compression/convert.h>
#include <acl/compression/track_array.h>
#include "Structs.h"

#define DLLEXPORT extern "C" __declspec(dllexport)
acl::ansi_allocator ACLAllocatorImpl;
using DecompContextDefault = acl::decompression_context<acl::decompression_settings>;

// math util
inline rtm::vector4f RTM_SIMD_CALL VectorCast(const FVector& Input) { return rtm::vector_set(Input.X, Input.Y, Input.Z); }
inline FVector RTM_SIMD_CALL VectorCast(rtm::vector4f_arg0 Input) { return FVector(rtm::vector_get_x(Input), rtm::vector_get_y(Input), rtm::vector_get_z(Input)); }
inline rtm::quatf RTM_SIMD_CALL QuatCast(const FQuat& Input) { return rtm::quat_set(Input.X, Input.Y, Input.Z, Input.W); }
inline FQuat RTM_SIMD_CALL QuatCast(rtm::quatf_arg0 Input) { return FQuat(rtm::quat_get_x(Input), rtm::quat_get_y(Input), rtm::quat_get_z(Input), rtm::quat_get_w(Input)); }