#pragma once
#include <acl/core/ansi_allocator.h>
#include <acl/core/compressed_tracks.h>
#include <acl/decompression/decompress.h>
#include "Structs.h"
#include "Framework.h"

acl::ansi_allocator ACLAllocatorImpl;
using DecompContextDefault = acl::decompression_context<acl::decompression_settings>;