#include "Framework.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#define COMP(val) if (strcmp(feature, val) == 0) { \
                        return 1; \
                    }

// Returns int (not bool): MSVC C++20+ may leave upper bits of RAX dirty when
// returning bool, which .NET unmanaged function pointers can misread as true.
DLLEXPORT int IsFeatureAvailable(const char* feature) {
    #if WITH_ACL
        COMP("ACL")
    #endif
    #if WITH_Oodle
        COMP("Oodle")
    #endif
    #if WITH_UEFORMAT
        COMP("UEFormat")
    #endif
        return 0;
}