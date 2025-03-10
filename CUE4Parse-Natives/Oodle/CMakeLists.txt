cmake_minimum_required(VERSION 3.10)
project(oodle2 CXX)

set(CMAKE_CXX_STANDARD 11)
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -mavx2")

include_directories(${PROJECT_SOURCE_DIR}/external/oodle2/core)
include_directories(${PROJECT_SOURCE_DIR}/external/oodle2/base)
include_directories(${PROJECT_SOURCE_DIR}/external/oodle2/core/public)

file(GLOB SOURCES ${PROJECT_SOURCE_DIR}/external/oodle2/core/*.cpp)
add_library(${PROJECT_NAME} STATIC ${SOURCES})

add_compile_definitions(OODLE_BUILDING_DLL)

# set_target_properties(${PROJECT_NAME} PROPERTIES 
#     POSITION_INDEPENDENT_CODE ON
#     VISIBILITY_INLINES_HIDDEN ON
#     C_VISIBILITY_PRESET default
#     CXX_VISIBILITY_PRESET default
# )
