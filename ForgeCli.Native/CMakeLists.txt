cmake_minimum_required(VERSION 3.16)
project(ForgeCliNative LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

add_executable(forgecli-native
    src/main.cpp
)

target_include_directories(forgecli-native PRIVATE
    ../SMLCore.Native/include
    ../SMSCore.Native/include
)

