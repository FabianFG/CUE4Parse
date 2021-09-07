cmake_minimum_required(VERSION 3.5)
project(CUE4Parse-Natives CXX)

set(CMAKE_CXX_STANDARD 11)

include_directories("${PROJECT_SOURCE_DIR}/common")

# ACL
include_directories("${PROJECT_SOURCE_DIR}/ACL/external/acl/includes")
include_directories("${PROJECT_SOURCE_DIR}/ACL/external/acl/external/rtm/includes")

add_library("${PROJECT_NAME}" SHARED ${PROJECT_SOURCE_DIR}/ACL/ACL.cpp)
set_target_properties("${PROJECT_NAME}" PROPERTIES PREFIX "")


if (WIN32)
    add_compile_definitions(WIN_EXPORT)
endif()

if (MSVC)
    install(FILES $<TARGET_PDB_FILE:${PROJECT_NAME}>
            DESTINATION "${PROJECT_SOURCE_DIR}/bin/Debug/" OPTIONAL) 
endif()

install(TARGETS CUE4Parse-Natives
        CONFIGURATIONS Debug
        RUNTIME DESTINATION "${PROJECT_SOURCE_DIR}/bin/Debug/")
install(TARGETS CUE4Parse-Natives
        CONFIGURATIONS Release
        RUNTIME DESTINATION "${PROJECT_SOURCE_DIR}/bin/Release/")