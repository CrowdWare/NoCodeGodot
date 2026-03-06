#pragma once

#include <cstdint>

#if defined(_WIN32)
#define SMS_EXPORT __declspec(dllexport)
#else
#define SMS_EXPORT __attribute__((visibility("default")))
#endif

extern "C" {
typedef int (*sms_native_ui_get_prop_fn)(
    const char* object_id,
    const char* property,
    char* out_json,
    int out_json_capacity,
    char* error,
    int error_capacity);

typedef int (*sms_native_ui_set_prop_fn)(
    const char* object_id,
    const char* property,
    const char* value_json,
    char* error,
    int error_capacity);

typedef int (*sms_native_ui_invoke_fn)(
    const char* object_id,
    const char* method,
    const char* args_json,
    char* out_json,
    int out_json_capacity,
    char* error,
    int error_capacity);

typedef int (*sms_native_sandbox_path_allow_fn)(
    const char* owner,
    const char* uri_path,
    char* error,
    int error_capacity);

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
SMS_EXPORT int sms_native_set_ui_callbacks(
    sms_native_ui_get_prop_fn get_prop,
    sms_native_ui_set_prop_fn set_prop,
    sms_native_ui_invoke_fn invoke,
    char* error,
    int error_capacity);
SMS_EXPORT int sms_native_set_sandbox_path_callback(
    sms_native_sandbox_path_allow_fn allow_path,
    char* error,
    int error_capacity);
}
