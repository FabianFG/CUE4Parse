cmake_minimum_required(VERSION 3.10)
project(CUE4Parse-Natives CXX)

set(CMAKE_CXX_STANDARD 11)


if (EXISTS "${PROJECT_SOURCE_DIR}/ACL/external/acl/includes")
    set(WITH_ACL 1)
endif()

if (EXISTS "${PROJECT_SOURCE_DIR}/Oodle/external/oodle2/core")
    set(WITH_Oodle 1)
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
        add_compile_definitions(OODLE_BUILDING_DLL)
        add_compile_definitions(WITH_Oodle)
        add_subdirectory("${PROJECT_SOURCE_DIR}/Oodle/")
endif()

set(SOURCES ${SOURCES} ${PROJECT_SOURCE_DIR}/Features.cpp)
include_directories("${PROJECT_SOURCE_DIR}/common")

add_library("${PROJECT_NAME}" SHARED ${SOURCES})
set_target_properties("${PROJECT_NAME}" PROPERTIES PREFIX "")

# Oodle
if (WITH_Oodle)
    target_link_libraries("${PROJECT_NAME}" PRIVATE oodle2)

    # Ensure all Oodle symbols are exported with the shared library
    if(WIN32)
        target_link_options("${PROJECT_NAME}" PRIVATE "/WHOLEARCHIVE:oodle2")
    elseif(APPLE)
        target_link_options("${PROJECT_NAME}" PRIVATE "-Wl,-force_load,$<TARGET_FILE:oodle2>")
    else() # Linux and other platforms
        target_link_options("${PROJECT_NAME}" PRIVATE "-Wl,--whole-archive" "$<TARGET_FILE:oodle2>" "-Wl,--no-whole-archive")
    endif()
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
