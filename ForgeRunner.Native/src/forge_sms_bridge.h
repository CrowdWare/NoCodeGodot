#pragma once

#include <cstdint>
#include <string>
#include <unordered_map>

namespace godot { class Control; }

namespace forge {

/// Maps SML id strings to live Godot Control pointers.
using IdMap = std::unordered_map<std::string, godot::Control*>;

/// Wraps the SMS native runtime library and provides real ui_get / ui_set /
/// ui_invoke callbacks so the SMS interpreter can read and write the Godot
/// Control tree.
///
/// A single SmsBridge instance lives in ForgeRunnerNativeMain.  The static
/// id_map() is shared so that the C-linkage callbacks can reach it without
/// needing a global pointer to the bridge object itself.
class SmsBridge {
public:
    SmsBridge();
    ~SmsBridge();

    SmsBridge(const SmsBridge&)            = delete;
    SmsBridge& operator=(const SmsBridge&) = delete;

    /// Load libsms_native from SMS_NATIVE_LIB_DIR env var or
    /// <repo_root>/SMSCore.Native/build.  Returns false if the library is
    /// unavailable (SMS execution is silently disabled).
    bool load(const std::string& repo_root);
    void unload();

    /// Create + load a session for the SMS script at @p script_path.
    /// Returns the session id, or -1 on failure.
    std::int64_t start_session(const std::string& script_path);

    /// Invoke an event on an object inside an active session.
    void dispatch_event(std::int64_t session,
                        const std::string& object_id,
                        const std::string& event_name,
                        const std::string& payload_json = "null");

    /// Dispose a session obtained from start_session().
    void dispose_session(std::int64_t session);

    bool loaded() const { return loaded_; }

    /// Global id → Control* map.  Populated by UiBuilder::apply_props(),
    /// cleared at the start of each UiBuilder::build() call.
    static IdMap& id_map();

private:
    bool  loaded_     = false;
    void* lib_handle_ = nullptr;

    using CreateFn  = int (*)(std::int64_t*, char*, int);
    using LoadFn    = int (*)(std::int64_t, const char*, char*, int);
    using InvokeFn  = int (*)(std::int64_t, const char*, const char*, const char*,
                               std::int64_t*, char*, int);
    using DisposeFn = int (*)(std::int64_t, char*, int);
    using SetUiCbFn = int (*)(
        int (*)(const char*, const char*, char*, int, char*, int),
        int (*)(const char*, const char*, const char*, char*, int),
        int (*)(const char*, const char*, const char*, char*, int, char*, int),
        char*, int);

    CreateFn  create_fn_    = nullptr;
    LoadFn    load_fn_      = nullptr;
    InvokeFn  invoke_fn_    = nullptr;
    DisposeFn dispose_fn_   = nullptr;
    SetUiCbFn set_ui_cb_fn_ = nullptr;
};

} // namespace forge
