cmake_minimum_required(VERSION 3.16)
project(sms_native LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

add_library(sms_native SHARED
    src/sms_native.cpp
)

target_include_directories(sms_native PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}/include
)

