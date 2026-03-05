#pragma once

#include <cstdint>

#if defined(_WIN32)
#define SMLCORE_EXPORT __declspec(dllexport)
#else
#define SMLCORE_EXPORT __attribute__((visibility("default")))
#endif

extern "C" {
SMLCORE_EXPORT int smlcore_native_parse(
    const char* source,
    std::int64_t* out_node_count,
    char* error,
    int error_capacity);

SMLCORE_EXPORT int smlcore_native_parse_ast_json(
    const char* source,
    char* out_json,
    int out_json_capacity,
    std::int64_t* out_json_length,
    char* error,
    int error_capacity);
}
