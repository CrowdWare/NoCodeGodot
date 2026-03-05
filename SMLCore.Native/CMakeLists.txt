cmake_minimum_required(VERSION 3.16)
project(smlcore_native LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

add_library(smlcore_native SHARED
    src/smlcore_native.cpp
)

target_include_directories(smlcore_native PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}/include
)

