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
SMS_EXPORT int sms_native_session_create(std::int64_t* out_session, char* error, int error_capacity);
SMS_EXPORT int sms_native_session_load(std::int64_t session, const char* source, char* error, int error_capacity);
SMS_EXPORT int sms_native_session_invoke(
    std::int64_t session,
    const char* target_id,
    const char* event_name,
    const char* args_json,
    std::int64_t* out_result,
    char* error,
    int error_capacity);
SMS_EXPORT int sms_native_session_dispose(std::int64_t session, char* error, int error_capacity);
}
