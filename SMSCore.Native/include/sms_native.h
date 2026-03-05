#pragma once

#include <cstdint>

#if defined(_WIN32)
#define SMS_EXPORT __declspec(dllexport)
#else
#define SMS_EXPORT __attribute__((visibility("default")))
#endif

extern "C" {
SMS_EXPORT int sms_native_execute(const char* source, std::int64_t* out_result, char* error, int error_capacity);
SMS_EXPORT int sms_native_sml_parse(const char* source, std::int64_t* out_node_count, char* error, int error_capacity);
}
