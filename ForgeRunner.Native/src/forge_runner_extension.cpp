#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/classes/node.hpp>
#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/core/defs.hpp>
#include <godot_cpp/godot.hpp>
#include <godot_cpp/variant/utility_functions.hpp>

#include <cstdlib>

using namespace godot;

class ForgeRunnerNativeMain : public Node {
    GDCLASS(ForgeRunnerNativeMain, Node);

protected:
    static void _bind_methods() {
        ClassDB::bind_method(D_METHOD("set_url", "url"), &ForgeRunnerNativeMain::set_url);
        ClassDB::bind_method(D_METHOD("get_url"), &ForgeRunnerNativeMain::get_url);
    }

public:
    void set_url(const String& url) {
        url_ = url;
    }

    String get_url() const {
        return url_;
    }

    void _ready() override {
        UtilityFunctions::print("[ForgeRunner.Native] native host bootstrap ready.");
        if (url_.is_empty()) {
            if (const char* env_url = std::getenv("FORGE_RUNNER_URL")) {
                url_ = String(env_url);
            }
        }
        if (!url_.is_empty()) {
            UtilityFunctions::print(String("[ForgeRunner.Native] host url=") + url_);
        }
    }

private:
    String url_;
};

void initialize_forge_runner_native(ModuleInitializationLevel p_level) {
    if (p_level != MODULE_INITIALIZATION_LEVEL_SCENE) {
        return;
    }

    ClassDB::register_class<ForgeRunnerNativeMain>();
}

void uninitialize_forge_runner_native(ModuleInitializationLevel p_level) {
    if (p_level != MODULE_INITIALIZATION_LEVEL_SCENE) {
        return;
    }
}

extern "C" {
GDExtensionBool GDE_EXPORT forge_runner_native_library_init(
    GDExtensionInterfaceGetProcAddress p_get_proc_address,
    const GDExtensionClassLibraryPtr p_library,
    GDExtensionInitialization* r_initialization) {
    GDExtensionBinding::InitObject init_obj(p_get_proc_address, p_library, r_initialization);
    init_obj.register_initializer(initialize_forge_runner_native);
    init_obj.register_terminator(uninitialize_forge_runner_native);
    init_obj.set_minimum_library_initialization_level(MODULE_INITIALIZATION_LEVEL_SCENE);
    return init_obj.init();
}
}
