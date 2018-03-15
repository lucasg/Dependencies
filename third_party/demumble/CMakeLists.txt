cmake_minimum_required(VERSION 3.1.0 FATAL_ERROR)
project(demumble C CXX)
add_definitions(-D_LIBCXXABI_FUNC_VIS=)

# TODO: Also do this for gcc once gcc 4.9 is common.
if (UNIX AND CMAKE_GENERATOR STREQUAL "Ninja" AND
    CMAKE_CXX_COMPILER_ID MATCHES "Clang")
  set(CMAKE_C_FLAGS "$CMAKE_C_FLAGS -fdiagnostics-color -Wall")
  set(CMAKE_CXX_FLAGS "$CMAKE_CXX_FLAGS -fdiagnostics-color -Wall")
endif()

# 10.9 chosen somewhat arbitrary; it's the first target where clang defaults
# to libc++ and ld64 defaults to stripping __TEXT,__eh_frame.
if (APPLE)
  set(CMAKE_C_FLAGS "$CMAKE_C_FLAGS -mmacosx-version-min=10.9")
  set(CMAKE_CXX_FLAGS "$CMAKE_CXX_FLAGS -mmacosx-version-min=10.9")
endif()

# This is apparently the simplest way to statically link the CRT in CMake:
if (WIN32)
  string(TOUPPER "${CMAKE_BUILD_TYPE}" build)
  foreach(lang C CXX)
    set(flag_var "CMAKE_${lang}_FLAGS_${build}")
    if(${flag_var} MATCHES "/MD")
      string(REGEX REPLACE "/MD" "/MT" ${flag_var} "${${flag_var}}")
    endif()
  endforeach()
endif()

add_executable(demumble
               demumble.cc
               third_party/wine/undname.c
               third_party/libcxxabi/cxa_demangle.cpp
)
set_target_properties(demumble PROPERTIES CXX_STANDARD 11
                                          CXX_STANDARD_REQUIRED ON)
