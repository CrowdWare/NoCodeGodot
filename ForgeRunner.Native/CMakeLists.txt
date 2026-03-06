cmake_minimum_required(VERSION 3.20)
project(ForgeRunnerNative LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

if(NOT DEFINED GODOT_CPP_DIR OR GODOT_CPP_DIR STREQUAL "")
    message(FATAL_ERROR "GODOT_CPP_DIR is required (path to godot-cpp checkout).")
endif()

set(GODOT_CPP_GEN_DIR "${GODOT_CPP_DIR}/gen")
set(GODOT_CPP_INCLUDE_DIR "${GODOT_CPP_DIR}/include")
set(GODOT_CPP_BINDING_DIR "${GODOT_CPP_DIR}/gdextension")

if(NOT EXISTS "${GODOT_CPP_INCLUDE_DIR}/godot_cpp/godot.hpp")
    message(FATAL_ERROR "godot-cpp headers not found in GODOT_CPP_DIR='${GODOT_CPP_DIR}'.")
endif()

if(WIN32)
    file(GLOB GODOT_CPP_LIB_CANDIDATES "${GODOT_CPP_DIR}/bin/libgodot-cpp*.lib")
else()
    file(GLOB GODOT_CPP_LIB_CANDIDATES "${GODOT_CPP_DIR}/bin/libgodot-cpp*.a")
endif()

list(LENGTH GODOT_CPP_LIB_CANDIDATES GODOT_CPP_LIB_COUNT)
if(GODOT_CPP_LIB_COUNT EQUAL 0)
    message(FATAL_ERROR "No godot-cpp library found in ${GODOT_CPP_DIR}/bin")
endif()
list(GET GODOT_CPP_LIB_CANDIDATES 0 GODOT_CPP_LIB)

add_library(forge_runner_native SHARED
    src/forge_runner_extension.cpp
)

target_include_directories(forge_runner_native PRIVATE
    "${GODOT_CPP_INCLUDE_DIR}"
    "${GODOT_CPP_GEN_DIR}/include"
    "${GODOT_CPP_BINDING_DIR}"
)

target_link_libraries(forge_runner_native PRIVATE "${GODOT_CPP_LIB}")

if(APPLE)
    target_link_options(forge_runner_native PRIVATE "-Wl,-undefined,dynamic_lookup")
endif()

add_executable(forge-runner-native
    src/main.cpp
)
