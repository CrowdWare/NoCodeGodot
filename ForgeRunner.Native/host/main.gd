extends Node

func _ready() -> void:
    var load_err: int = GDExtensionManager.load_extension("res://forge_runner_native.gdextension")
    if load_err != OK and load_err != ERR_ALREADY_IN_USE:
        push_error("Failed to load forge_runner_native.gdextension (error %d)." % load_err)

    var url: String = OS.get_environment("FORGE_RUNNER_URL")
    if url.is_empty():
        url = "(none)"

    var native_node: Variant = ClassDB.instantiate("ForgeRunnerNativeMain")
    if native_node == null:
        push_error("ForgeRunnerNativeMain class not registered (gdextension not loaded).")
    else:
        if native_node is Node:
            if native_node.has_method("set_url"):
                native_node.call("set_url", url)
            add_child(native_node)
        else:
            push_error("ForgeRunnerNativeMain instantiate returned non-Node.")

    var root_window: Window = get_window()
    if root_window != null:
        root_window.title = "ForgeRunner.Native Host"

    var layer := CanvasLayer.new()
    add_child(layer)

    var panel := PanelContainer.new()
    panel.anchor_right = 1.0
    panel.anchor_bottom = 0.0
    panel.offset_left = 12.0
    panel.offset_top = 12.0
    panel.offset_right = -12.0
    panel.offset_bottom = 96.0
    layer.add_child(panel)

    var box := VBoxContainer.new()
    panel.add_child(box)

    var title := Label.new()
    title.text = "ForgeRunner.Native window host"
    box.add_child(title)

    var subtitle := Label.new()
    subtitle.text = "URL: %s" % url
    subtitle.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
    box.add_child(subtitle)
