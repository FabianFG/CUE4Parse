#pragma once

#if defined _WIN32 || defined __CYGWIN__
  #ifdef WIN_EXPORT
    #ifdef __GNUC__
      #define DLLEXPORT extern "C" __attribute__ ((dllexport))
    #else
      #define DLLEXPORT extern "C" __declspec(dllexport)
    #endif
  #else
    #ifdef __GNUC__
      #define DLLEXPORT __attribute__ ((dllimport))
    #else
      #define DLLEXPORT __declspec(dllimport)
    #endif
  #endif
  #define NOEXPORT
#else
  #if __GNUC__ >= 4
    #define DLLEXPORT extern "C" __attribute__ ((visibility ("default")))
    #define NOEXPORT  extern "C" __attribute__ ((visibility ("hidden")))
  #else
    #define DLLEXPORT
    #define NOEXPORT
  #endif
#endif