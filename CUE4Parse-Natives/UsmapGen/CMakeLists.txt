set(USMAPGEN_BUILD_TOOLS OFF CACHE BOOL "Build the UsmapGen CLI" FORCE)
set(USMAPGEN_BUILD_TESTS OFF CACHE BOOL "Build the UsmapGen tests" FORCE)
set(USMAPGEN_OODLE OFF CACHE BOOL "Use UsmapGen's bundled Oodle compressor" FORCE)
add_subdirectory(external/UsmapGen EXCLUDE_FROM_ALL)

if(MSVC)
    set_property(TARGET UsmapGen PROPERTY COMPILE_OPTIONS /EHsc)
endif()

target_sources(CUE4Parse-Natives PRIVATE "UsmapGen.cpp" UsmapGenNotices.rc)
target_include_directories(CUE4Parse-Natives PRIVATE "$<$<COMPILE_LANGUAGE:RC>:${CMAKE_CURRENT_SOURCE_DIR}>")
target_link_libraries(CUE4Parse-Natives PRIVATE UsmapGen::UsmapGen)
target_compile_definitions(CUE4Parse-Natives PRIVATE WITH_USMAPGEN)
