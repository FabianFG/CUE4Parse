#include "Framework.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#define COMP(val) if (strcmp(feature, val) == 0) { \
                        return true; \
                    }

DLLEXPORT bool IsFeatureAvailable(const char* feature) {
    #if WITH_ACL
        COMP("ACL")
    #endif
    #if WITH_Oodle
        COMP("Oodle")
    #endif
        return false;
}