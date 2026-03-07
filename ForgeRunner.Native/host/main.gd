extends Node

var _native_node: Node = null
var _content_root: Control = null
var _current_sml_path: String = ""
var _splash_timer: Timer = null
var _pending_next_path: String = ""

func _ready() -> void:
	_load_extension()
	_attach_native_main()

	var start_url: String = OS.get_environment("FORGE_RUNNER_URL")
	if start_url.is_empty():
		_push_host_error("No FORGE_RUNNER_URL set.")
		return

	var entry_path := _resolve_entry_path_from_url(start_url)
	if entry_path.is_empty():
		_push_host_error("Unsupported URL: %s" % start_url)
		return

	_show_sml_path(entry_path)


func _load_extension() -> void:
	var load_err: int = GDExtensionManager.load_extension("res://forge_runner_native.gdextension")
	if load_err != OK and load_err != ERR_ALREADY_IN_USE:
		push_error("Failed to load forge_runner_native.gdextension (error %d)." % load_err)


func _attach_native_main() -> void:
	var instance: Variant = ClassDB.instantiate("ForgeRunnerNativeMain")
	if instance == null:
		push_error("ForgeRunnerNativeMain class not registered (gdextension not loaded).")
		return
	if instance is Node:
		_native_node = instance
		var start_url: String = OS.get_environment("FORGE_RUNNER_URL")
		if not start_url.is_empty() and _native_node.has_method("set_url"):
			_native_node.call("set_url", start_url)
		add_child(_native_node)
	else:
		push_error("ForgeRunnerNativeMain instantiate returned non-Node.")


func _resolve_entry_path_from_url(url: String) -> String:
	if url.begins_with("file://"):
		var path := _file_url_to_path(url)
		if path.is_empty():
			return ""
		if path.get_file().to_lower() == "manifest.sml":
			return _resolve_manifest_entry(path)
		return path
	return ""


func _resolve_manifest_entry(manifest_path: String) -> String:
	if not FileAccess.file_exists(manifest_path):
		return ""
	var text := FileAccess.get_file_as_string(manifest_path)
	if text.is_empty():
		return ""
	var entry := _match_string(text, '(?m)^\\s*entry\\s*:\\s*"([^"]+)"')
	if entry.is_empty():
		entry = "app.sml"
	return ProjectSettings.globalize_path(manifest_path.get_base_dir().path_join(entry))


func _file_url_to_path(url: String) -> String:
	var raw := url.substr("file://".length())
	if raw.is_empty():
		return ""
	if raw.begins_with("localhost/"):
		raw = raw.substr("localhost/".length())
	if not raw.begins_with("/"):
		raw = "/" + raw
	return ProjectSettings.globalize_path(raw)


func _show_sml_path(path: String) -> void:
	if not FileAccess.file_exists(path):
		_push_host_error("SML file missing: %s" % path)
		return
	var text := FileAccess.get_file_as_string(path)
	if text.is_empty():
		_push_host_error("SML file empty or unreadable: %s" % path)
		return
	_current_sml_path = path
	_render_document(text, path)


func _render_document(text: String, path: String) -> void:
	var root_name := _match_string(text, '(?m)^\\s*([A-Za-z_][A-Za-z0-9_.]*)\\s*\\{')
	if root_name.is_empty():
		_push_host_error("Failed to parse SML root: %s" % path)
		return

	_clear_content()
	var root := Control.new()
	root.anchor_right = 1.0
	root.anchor_bottom = 1.0
	add_child(root)
	_content_root = root

	var title := _match_string(text, '(?m)^\\s*title\\s*:\\s*"([^"]+)"')
	var size_match := _match_size(text)
	var root_lower := root_name.to_lower()
	var window := get_window()
	if window != null:
		if not title.is_empty():
			window.title = title
		elif root_lower == "splashscreen":
			window.title = "Forge Splash"
		else:
			window.title = "ForgeRunner.Native Host"
		if size_match.x > 0 and size_match.y > 0:
			window.size = size_match
		window.visible = (root_lower == "window" or root_lower == "splashscreen")

	_draw_minimal_root(root, root_name, path)

	if root_lower == "splashscreen":
		var load_on_ready := _match_string(text, '(?m)^\\s*loadOnReady\\s*:\\s*"([^"]+)"')
		var duration_text := _match_string(text, '(?m)^\\s*duration\\s*:\\s*([0-9]+)')
		var duration_ms := 3500
		if not duration_text.is_empty():
			duration_ms = max(1, int(duration_text))
		if not load_on_ready.is_empty():
			_pending_next_path = path.get_base_dir().path_join(load_on_ready)
			_schedule_splash_next(duration_ms)


func _draw_minimal_root(parent: Control, root_name: String, path: String) -> void:
	var card := PanelContainer.new()
	card.anchor_left = 0.5
	card.anchor_top = 0.5
	card.anchor_right = 0.5
	card.anchor_bottom = 0.5
	card.offset_left = -260.0
	card.offset_top = -60.0
	card.offset_right = 260.0
	card.offset_bottom = 60.0
	parent.add_child(card)

	var box := VBoxContainer.new()
	box.add_theme_constant_override("separation", 4)
	card.add_child(box)

	var title := Label.new()
	title.text = "Loaded SML root: %s" % root_name
	box.add_child(title)

	var sub := Label.new()
	sub.text = path
	sub.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	box.add_child(sub)


func _schedule_splash_next(duration_ms: int) -> void:
	if _splash_timer != null:
		_splash_timer.queue_free()
		_splash_timer = null
	_splash_timer = Timer.new()
	_splash_timer.one_shot = true
	_splash_timer.wait_time = float(duration_ms) / 1000.0
	_splash_timer.timeout.connect(_on_splash_timeout)
	add_child(_splash_timer)
	_splash_timer.start()


func _on_splash_timeout() -> void:
	if _pending_next_path.is_empty():
		return
	var next_path := _pending_next_path
	_pending_next_path = ""
	_show_sml_path(next_path)


func _match_string(text: String, pattern: String) -> String:
	var regex := RegEx.new()
	var err := regex.compile(pattern)
	if err != OK:
		return ""
	var result := regex.search(text)
	if result == null or result.get_group_count() < 1:
		return ""
	return result.get_string(1)


func _match_size(text: String) -> Vector2i:
	var regex := RegEx.new()
	if regex.compile('(?m)^\\s*size\\s*:\\s*([0-9]+)\\s*,\\s*([0-9]+)') != OK:
		return Vector2i.ZERO
	var result := regex.search(text)
	if result == null:
		return Vector2i.ZERO
	var w := int(result.get_string(1))
	var h := int(result.get_string(2))
	if w <= 0 or h <= 0:
		return Vector2i.ZERO
	return Vector2i(w, h)


func _clear_content() -> void:
	if _content_root != null:
		_content_root.queue_free()
		_content_root = null


func _push_host_error(text: String) -> void:
	push_error(text)
	_clear_content()
	var layer := CanvasLayer.new()
	add_child(layer)
	var label := Label.new()
	label.text = text
	label.position = Vector2(16, 16)
	layer.add_child(label)
