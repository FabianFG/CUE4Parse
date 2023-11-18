cmake_minimum_required(VERSION 3.5)
project(CUE4Parse-Natives CXX)

set(CMAKE_CXX_STANDARD 17)

set(CMAKE_MODULE_PATH "${PROJECT_SOURCE_DIR}/CMakeModules")


if (EXISTS "${PROJECT_SOURCE_DIR}/ACL/external/acl/includes")
    set(WITH_ACL 1)
else()
    # include(FetchContent)
    # FetchContent_Declare(acl
    #   GIT_REPOSITORY "https://github.com/nfrechette/acl/"
    #   GIT_TAG "dadbbb3890da7c7c064ff11d212ad79da848fcb4"   # it's much better to use a specific Git revision or Git tag for reproducibility
    #   SOURCE_DIR "${CMAKE_CURRENT_LIST_DIR}/ACL/external/acl"
    # )
    # FetchContent_MakeAvailable(acl)
    # set(WITH_ACL 1)
endif()

if (EXISTS ${PROJECT_SOURCE_DIR}/Oodle/external/oodle2/core)
    set(WITH_Oodle 1)
endif()

find_package(FBX)
if (FBX_FOUND)
    message("Found FBX at ${FBX_INCLUDE_DIR}")
    set(WITH_FBX 1)
else()
    message("FBX sdk not found")
endif()


# ACL
if (WITH_ACL)
        add_compile_definitions(WITH_ACL)
        include_directories("${PROJECT_SOURCE_DIR}/ACL/external/acl/includes")
        include_directories("${PROJECT_SOURCE_DIR}/ACL/external/acl/external/rtm/includes")
        set(SOURCES ${SOURCES} ${PROJECT_SOURCE_DIR}/ACL/ACL.cpp)
endif()

# Oodle
if (WITH_Oodle)
        include_directories(${PROJECT_SOURCE_DIR}/Oodle/external/oodle2/core)
        include_directories(${PROJECT_SOURCE_DIR}/Oodle/external/oodle2/base)
        include_directories(${PROJECT_SOURCE_DIR}/Oodle/external/oodle2/core/public)
        file(GLOB OODLE_SOURCES ${PROJECT_SOURCE_DIR}/Oodle/external/oodle2/core/*.cpp)

        set(SOURCES ${SOURCES} ${PROJECT_SOURCE_DIR}/Oodle/Oodle.cpp ${OODLE_SOURCES})

        add_compile_definitions(OODLE_BUILDING_LIB)
        add_compile_definitions(WITH_Oodle)
endif()

if (WITH_FBX)
    include_directories(${FBX_INCLUDE_DIR})
    file(GLOB FBX_SOURCES ${PROJECT_SOURCE_DIR}/FBX/*.cpp)
    set(SOURCES ${SOURCES} ${FBX_SOURCES})

    add_compile_definitions(WITH_FBX)
endif()


set(SOURCES ${SOURCES} ${PROJECT_SOURCE_DIR}/Features.cpp)
include_directories("${PROJECT_SOURCE_DIR}/common")


add_library("${PROJECT_NAME}" SHARED ${SOURCES})
set_target_properties("${PROJECT_NAME}" PROPERTIES PREFIX "")
set_target_properties("${PROJECT_NAME}" PROPERTIES LINK_FLAGS "/ignore:4099") # supress pdb warning because we might not have fbx pdb

if (WITH_FBX)
    target_link_libraries(${PROJECT_NAME} optimized ${FBX_LIBRARY}      debug ${FBX_LIBRARY_DEBUG}
                                          optimized ${FBX_ZLIB_LIBRARY} debug ${FBX_ZLIB_LIBRARY_DEBUG}
                                          optimized ${FBX_XML2_LIBRARY} debug ${FBX_XML2_LIBRARY_DEBUG})
endif()


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