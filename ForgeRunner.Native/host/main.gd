extends Node

class PosingEditorControl:
	extends SubViewportContainer

	var _viewport: SubViewport = null
	var _root_3d: Node3D = null
	var _camera: Camera3D = null
	var _model_node: Node = null
	var _yaw_degrees: float = 0.0
	var _pitch_degrees: float = -18.0
	var _distance: float = 6.0
	var _dragging: bool = false

	func _ready() -> void:
		_ensure_scene()
		_on_resized()

	func _gui_input(event: InputEvent) -> void:
		if event is InputEventMouseButton:
			var mb: InputEventMouseButton = event
			if mb.button_index == MOUSE_BUTTON_LEFT:
				_dragging = mb.pressed
				return
			if mb.pressed and mb.button_index == MOUSE_BUTTON_WHEEL_UP:
				_distance = maxf(1.5, _distance - 0.4)
				_update_camera()
				return
			if mb.pressed and mb.button_index == MOUSE_BUTTON_WHEEL_DOWN:
				_distance = minf(20.0, _distance + 0.4)
				_update_camera()
				return

		if event is InputEventMouseMotion and _dragging:
			var mm: InputEventMouseMotion = event
			_yaw_degrees -= mm.relative.x * 0.35
			_pitch_degrees -= mm.relative.y * 0.25
			_pitch_degrees = clampf(_pitch_degrees, -80.0, 80.0)
			_update_camera()

	func _notification(what: int) -> void:
		if what == NOTIFICATION_RESIZED:
			_on_resized()

	func set_model_source(path: String) -> void:
		_ensure_scene()
		_reload_model(path)

	func _ensure_scene() -> void:
		if _viewport != null:
			return
		stretch = false
		mouse_filter = Control.MOUSE_FILTER_STOP
		_viewport = SubViewport.new()
		_viewport.name = "EditorViewport"
		_viewport.size = Vector2i(1280, 720)
		_viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
		_viewport.render_target_clear_mode = SubViewport.CLEAR_MODE_ALWAYS
		_viewport.disable_3d = false
		add_child(_viewport)

		_root_3d = Node3D.new()
		_viewport.add_child(_root_3d)

		_camera = Camera3D.new()
		_camera.current = true
		_root_3d.add_child(_camera)

		var light := DirectionalLight3D.new()
		light.light_energy = 2.2
		light.rotation_degrees = Vector3(-48.0, -36.0, 0.0)
		_root_3d.add_child(light)

		var floor := MeshInstance3D.new()
		var floor_mesh := PlaneMesh.new()
		floor_mesh.size = Vector2(12.0, 12.0)
		floor.mesh = floor_mesh
		var floor_mat := StandardMaterial3D.new()
		floor_mat.albedo_color = Color(0.18, 0.20, 0.24, 1.0)
		floor.material_override = floor_mat
		floor.rotation_degrees = Vector3(-90.0, 0.0, 0.0)
		_root_3d.add_child(floor)

		_update_camera()

	func _on_resized() -> void:
		if _viewport == null:
			return
		var s := size
		var w: int = maxi(64, int(s.x))
		var h: int = maxi(64, int(s.y))
		_viewport.size = Vector2i(w, h)

	func _update_camera() -> void:
		if _camera == null:
			return
		var yaw := deg_to_rad(_yaw_degrees)
		var pitch := deg_to_rad(_pitch_degrees)
		var target := Vector3(0.0, 1.0, 0.0)
		var pos := Vector3(
			target.x + _distance * cos(pitch) * sin(yaw),
			target.y + _distance * sin(pitch),
			target.z + _distance * cos(pitch) * cos(yaw)
		)
		_camera.look_at_from_position(pos, target, Vector3.UP, false)

	func _reload_model(path: String) -> void:
		if _root_3d == null:
			return
		if _model_node != null:
			_model_node.queue_free()
			_model_node = null

		if path.is_empty() or not FileAccess.file_exists(path):
			var dummy := MeshInstance3D.new()
			var capsule := CapsuleMesh.new()
			capsule.radius = 0.35
			capsule.height = 1.4
			dummy.mesh = capsule
			dummy.position = Vector3(0.0, 0.9, 0.0)
			_root_3d.add_child(dummy)
			_model_node = dummy
			return

		var lower := path.to_lower()
		if lower.ends_with(".glb") or lower.ends_with(".gltf"):
			var gltf_doc := GLTFDocument.new()
			var gltf_state := GLTFState.new()
			var gltf_err: int = gltf_doc.append_from_file(path, gltf_state)
			if gltf_err == OK:
				var generated: Node = gltf_doc.generate_scene(gltf_state)
				if generated != null:
					_root_3d.add_child(generated)
					_model_node = generated
					return

		var loaded := ResourceLoader.load(path)
		if loaded is PackedScene:
			var inst := (loaded as PackedScene).instantiate()
			if inst != null:
				_root_3d.add_child(inst)
				_model_node = inst
				return
		if loaded is Mesh:
			var mi := MeshInstance3D.new()
			mi.mesh = loaded
			_root_3d.add_child(mi)
			_model_node = mi
			return

class DockingHostControl:
	extends Container

	const META_DOCK_SIDE := "forge_dock_side"
	const META_DOCK_FIXED_WIDTH := "forge_dock_fixed_width"
	const META_DOCK_FIXED_HEIGHT := "forge_dock_fixed_height"
	const META_DOCK_HEIGHT_PERCENT := "forge_dock_height_percent"
	const META_DOCK_FLEX := "forge_dock_flex"
	const META_DOCK_GAP := "forge_dock_gap"

	func _notification(what: int) -> void:
		if what == NOTIFICATION_SORT_CHILDREN:
			_arrange_children()

	func _arrange_children() -> void:
		var left_top: Control = null
		var left_bottom: Control = null
		var center_nodes: Array[Control] = []
		var right_top: Control = null
		var right_bottom: Control = null

		for i in range(get_child_count()):
			var child := get_child(i)
			if not (child is Control):
				continue
			var c := child as Control
			if not c.visible:
				continue
			var side := _dock_side_of(c)
			match side:
				"left":
					left_top = c
				"leftbottom":
					left_bottom = c
				"right":
					right_top = c
				"rightbottom":
					right_bottom = c
				_:
					center_nodes.append(c)

		var gap := 0.0
		if has_meta(META_DOCK_GAP):
			gap = maxf(0.0, float(get_meta(META_DOCK_GAP)))

		var total_w := size.x
		var total_h := size.y
		var has_left := left_top != null or left_bottom != null
		var has_right := right_top != null or right_bottom != null

		var left_w := 0.0
		if has_left:
			left_w = _column_width(left_top, left_bottom, 240.0)
		var right_w := 0.0
		if has_right:
			right_w = _column_width(right_top, right_bottom, 240.0)

		var used_gap := 0.0
		if has_left:
			used_gap += gap
		if has_right:
			used_gap += gap

		var center_w := maxf(0.0, total_w - left_w - right_w - used_gap)
		var x := 0.0
		if has_left:
			_layout_column(left_top, left_bottom, Rect2(x, 0.0, left_w, total_h), gap)
			x += left_w + gap
		if center_nodes.size() > 0:
			var center_rect := Rect2(x, 0.0, center_w, total_h)
			var first_center: Control = center_nodes[0]
			fit_child_in_rect(first_center, center_rect)
			for idx in range(1, center_nodes.size()):
				var hidden_center: Control = center_nodes[idx]
				fit_child_in_rect(hidden_center, Rect2(center_rect.position, Vector2(0.0, 0.0)))
			x += center_w + (gap if has_right else 0.0)
		if has_right:
			_layout_column(right_top, right_bottom, Rect2(x, 0.0, right_w, total_h), gap)

	func _layout_column(top: Control, bottom: Control, rect: Rect2, gap: float) -> void:
		if top != null and bottom != null:
			var bottom_h := _resolved_bottom_height(bottom, rect.size.y)
			var top_h := maxf(0.0, rect.size.y - bottom_h - gap)
			bottom_h = maxf(0.0, rect.size.y - top_h - gap)
			fit_child_in_rect(top, Rect2(rect.position, Vector2(rect.size.x, top_h)))
			var by := rect.position.y + top_h + gap
			fit_child_in_rect(bottom, Rect2(Vector2(rect.position.x, by), Vector2(rect.size.x, bottom_h)))
			return
		if top != null:
			fit_child_in_rect(top, rect)
			return
		if bottom != null:
			fit_child_in_rect(bottom, rect)

	func _resolved_bottom_height(container: Control, total_h: float) -> float:
		var fixed_h := _dock_number_of(container, META_DOCK_FIXED_HEIGHT, -1.0)
		if fixed_h > 0.0:
			return minf(total_h, fixed_h)
		var percent := _dock_number_of(container, META_DOCK_HEIGHT_PERCENT, -1.0)
		if percent > 0.0:
			return clampf(total_h * (percent / 100.0), 0.0, total_h)
		return total_h * 0.42

	func _column_width(top: Control, bottom: Control, fallback: float) -> float:
		var width := fallback
		if top != null:
			width = _dock_number_of(top, META_DOCK_FIXED_WIDTH, width)
		if bottom != null:
			width = _dock_number_of(bottom, META_DOCK_FIXED_WIDTH, width)
		return maxf(1.0, width)

	func _dock_side_of(control: Control) -> String:
		if control.has_meta(META_DOCK_SIDE):
			return String(control.get_meta(META_DOCK_SIDE)).to_lower().strip_edges()
		return "center"

	func _dock_number_of(control: Control, key: String, fallback: float) -> float:
		if not control.has_meta(key):
			return fallback
		return float(control.get_meta(key))

class DockingContainerControl:
	extends TabContainer
class SmlNode:
	var name: String = ""
	var props: Dictionary = {}
	var children: Array = []

var _native_node: Node = null
var _content_root: Control = null
var _splash_timer: Timer = null
var _pending_next_path: String = ""

var _src: String = ""
var _pos: int = 0
var _strings: Dictionary = {}

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
	_render_document(text, path)

func _render_document(text: String, path: String) -> void:
	var parsed := _parse_sml(text)
	if parsed == null:
		_push_host_error("Failed to parse SML root: %s" % path)
		return

	_clear_content()
	_strings = _load_strings_map(path.get_base_dir())
	var root_container := Control.new()
	root_container.anchor_right = 1.0
	root_container.anchor_bottom = 1.0
	add_child(root_container)
	_content_root = root_container

	_apply_window_from_root(parsed)

	var ui_root: Control = _build_ui_tree(parsed, path.get_base_dir())
	if ui_root != null:
		ui_root.anchor_right = 1.0
		ui_root.anchor_bottom = 1.0
		root_container.add_child(ui_root)

	if parsed.name.to_lower() == "splashscreen":
		var load_on_ready := String(parsed.props.get("loadOnReady", ""))
		var duration_ms := 3500
		if parsed.props.has("duration"):
			duration_ms = max(1, int(String(parsed.props["duration"])))
		if not load_on_ready.is_empty():
			_pending_next_path = path.get_base_dir().path_join(load_on_ready)
			_schedule_splash_next(duration_ms)

func _apply_window_from_root(root: SmlNode) -> void:
	var w := get_window()
	if w == null:
		return
	var root_lower := root.name.to_lower()
	var title := String(root.props.get("title", ""))
	if title.begins_with("@"):
		title = "Forge"
	if title.is_empty():
		title = "ForgeRunner.Native Host"
	if root_lower == "splashscreen":
		title = "Forge Splash"
	w.title = title
	if root.props.has("size"):
		var sz := _parse_size_tuple(String(root.props["size"]))
		if sz.x > 0 and sz.y > 0:
			w.size = sz
	if root.props.has("minSize"):
		var min_sz := _parse_size_tuple(String(root.props["minSize"]))
		if min_sz.x > 0 and min_sz.y > 0:
			w.min_size = min_sz
	w.visible = (root_lower == "window" or root_lower == "splashscreen")

func _build_ui_tree(node: SmlNode, base_dir: String) -> Control:
	var control := _create_control_for_node(node)
	if control == null:
		return null
	_apply_common_properties(control, node)
	_apply_specific_properties(control, node, base_dir)

	var name_lower := node.name.to_lower()
	if name_lower == "dockinghost" and control is DockingHostControl:
		for child in node.children:
			if not (child is SmlNode):
				continue
			var child_control := _build_ui_tree(child, base_dir)
			if child_control != null:
				control.add_child(child_control)
		return control

	if name_lower == "dockingcontainer" and control is DockingContainerControl:
		var tabs: DockingContainerControl = control
		var docking_id := String(node.props.get("id", ""))
		var tab_index := 0
		for child in node.children:
			if not (child is SmlNode):
				continue
			var child_control := _build_ui_tree(child, base_dir)
			if child_control != null:
				child_control.size_flags_horizontal = Control.SIZE_EXPAND_FILL
				child_control.size_flags_vertical = Control.SIZE_EXPAND_FILL
				tabs.add_child(child_control)
				tabs.set_tab_title(tab_index, _resolve_docking_tab_title(child, docking_id))
				tab_index += 1
		return control

	if name_lower == "menubar" and control is HBoxContainer:
		for child in node.children:
			if not (child is SmlNode):
				continue
			if child.name.to_lower() != "popupmenu":
				continue
			var menu_button := MenuButton.new()
			menu_button.text = String(child.props.get("title", "Menu"))
			if menu_button.text.begins_with("@"):
				menu_button.text = menu_button.text.trim_prefix("@").get_slice(".", -1)
			var popup := menu_button.get_popup()
			var item_id := 1
			for item in child.children:
				if not (item is SmlNode):
					continue
				if item.name.to_lower() != "item":
					continue
				var text_value := String(item.props.get("text", "Item"))
				if text_value.begins_with("@"):
					text_value = text_value.trim_prefix("@").get_slice(".", -1)
				popup.add_item(text_value, item_id)
				item_id += 1
			control.add_child(menu_button)
		return control

	for child in node.children:
		if not (child is SmlNode):
			continue
		if child.name.to_lower() == "item":
			continue
		var child_control := _build_ui_tree(child, base_dir)
		if child_control != null:
			_auto_layout_child(control, child, child_control)
			control.add_child(child_control)

	return control

func _auto_layout_child(parent_control: Control, child_node: SmlNode, child_control: Control) -> void:
	var has_h_flag := child_node.props.has("sizeFlagsHorizontal")
	var has_v_flag := child_node.props.has("sizeFlagsVertical")
	var has_fixed_width := child_node.props.has("fixedWidth") or child_node.props.has("width")
	var has_fixed_height := child_node.props.has("fixedHeight") or child_node.props.has("height")

	if parent_control is HBoxContainer:
		if not has_h_flag and not has_fixed_width and child_control.custom_minimum_size.x <= 0.0:
			child_control.size_flags_horizontal = Control.SIZE_EXPAND_FILL

	if parent_control is VBoxContainer:
		if not has_v_flag and not has_fixed_height and child_control.custom_minimum_size.y <= 0.0:
			child_control.size_flags_vertical = Control.SIZE_EXPAND_FILL

func _create_control_for_node(node: SmlNode) -> Control:
	var name := node.name.to_lower()
	match name:
		"window", "splashscreen":
			return Control.new()
		"vboxcontainer":
			return VBoxContainer.new()
		"hboxcontainer":
			return HBoxContainer.new()
		"control":
			return Control.new()
		"label":
			return Label.new()
		"lineedit", "numberpicker":
			return LineEdit.new()
		"button", "windowdrag", "ui.navtab":
			return Button.new()
		"texturebutton":
			return TextureButton.new()
		"optionbutton":
			return OptionButton.new()
		"texturerect":
			return TextureRect.new()
		"progressbar":
			return ProgressBar.new()
		"tree":
			return Tree.new()
		"codeedit":
			return CodeEdit.new()
		"markdown":
			var md := RichTextLabel.new()
			md.fit_content = false
			md.scroll_active = true
			return md
		"linkbutton":
			return LinkButton.new()
		"hseparator":
			return HSeparator.new()
		"vseparator":
			return VSeparator.new()
		"vsplitcontainer":
			return VSplitContainer.new()
		"menubar":
			return HBoxContainer.new()
		"popupmenu":
			return Control.new()
		"dockinghost":
			return DockingHostControl.new()
		"dockingcontainer":
			return DockingContainerControl.new()
		"timeline":
			return VBoxContainer.new()
		"posingeditor":
			var pe := PosingEditorControl.new()
			pe.size_flags_horizontal = Control.SIZE_EXPAND_FILL
			pe.size_flags_vertical = Control.SIZE_EXPAND_FILL
			return pe
		_:
			if name.find(".") >= 0:
				return Button.new()
			return Control.new()

func _apply_common_properties(control: Control, node: SmlNode) -> void:
	if node.props.has("id"):
		control.name = String(node.props["id"])

	if node.props.has("visible"):
		control.visible = _parse_bool(String(node.props["visible"]), true)

	if node.props.has("anchors"):
		var a := String(node.props["anchors"]).to_lower()
		var l := a.find("left") >= 0
		var t := a.find("top") >= 0
		var r := a.find("right") >= 0
		var b := a.find("bottom") >= 0
		if l:
			control.anchor_left = 0.0
		if t:
			control.anchor_top = 0.0
		if r:
			control.anchor_right = 1.0
		if b:
			control.anchor_bottom = 1.0
		if l and t and r and b:
			control.offset_left = 0.0
			control.offset_top = 0.0
			control.offset_right = 0.0
			control.offset_bottom = 0.0

	if node.props.has("left"):
		control.offset_left = float(int(String(node.props["left"])))
	if node.props.has("top"):
		control.offset_top = float(int(String(node.props["top"])))
	if node.props.has("right"):
		control.offset_right = float(int(String(node.props["right"])))
	if node.props.has("bottom"):
		control.offset_bottom = float(int(String(node.props["bottom"])))
	if node.props.has("offsetLeft"):
		control.offset_left = float(int(String(node.props["offsetLeft"])))
	if node.props.has("offsetTop"):
		control.offset_top = float(int(String(node.props["offsetTop"])))
	if node.props.has("offsetRight"):
		control.offset_right = float(int(String(node.props["offsetRight"])))
	if node.props.has("offsetBottom"):
		control.offset_bottom = float(int(String(node.props["offsetBottom"])))

	if node.props.has("width") or node.props.has("height"):
		var w := control.custom_minimum_size.x
		var h := control.custom_minimum_size.y
		if node.props.has("width"):
			w = float(int(String(node.props["width"])))
		if node.props.has("height"):
			h = float(int(String(node.props["height"])))
		control.custom_minimum_size = Vector2(w, h)

	if node.props.has("sizeFlagsHorizontal"):
		var hflags := String(node.props["sizeFlagsHorizontal"]).to_lower()
		if hflags.find("expandfill") >= 0:
			control.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	if node.props.has("sizeFlagsVertical"):
		var vflags := String(node.props["sizeFlagsVertical"]).to_lower()
		if vflags.find("expandfill") >= 0:
			control.size_flags_vertical = Control.SIZE_EXPAND_FILL

func _apply_specific_properties(control: Control, node: SmlNode, base_dir: String) -> void:
	if control is Label:
		if node.props.has("text"):
			(control as Label).text = _resolve_text(String(node.props["text"]))
		if node.props.has("wrap") and _parse_bool(String(node.props["wrap"]), false):
			(control as Label).autowrap_mode = TextServer.AUTOWRAP_WORD_SMART

	if control is LineEdit:
		if node.props.has("text"):
			(control as LineEdit).text = _resolve_text(String(node.props["text"]))
		if node.props.has("placeholderText"):
			(control as LineEdit).placeholder_text = _resolve_text(String(node.props["placeholderText"]))

	if control is Button and not (control is MenuButton):
		if node.props.has("text"):
			(control as Button).text = _resolve_text(String(node.props["text"]))

	if control is LinkButton:
		if node.props.has("text"):
			(control as LinkButton).text = _resolve_text(String(node.props["text"]))

	if control is RichTextLabel:
		if node.props.has("text"):
			(control as RichTextLabel).text = _resolve_text(String(node.props["text"]))
		if node.props.has("src"):
			var p := _resolve_resource_uri(String(node.props["src"]), base_dir)
			if FileAccess.file_exists(p):
				(control as RichTextLabel).text = FileAccess.get_file_as_string(p)

	if control is ProgressBar:
		if node.props.has("min"):
			(control as ProgressBar).min_value = float(int(String(node.props["min"])))
		if node.props.has("max"):
			(control as ProgressBar).max_value = float(int(String(node.props["max"])))
		if node.props.has("value"):
			(control as ProgressBar).value = float(int(String(node.props["value"])))
		if node.props.has("showPercentage"):
			(control as ProgressBar).show_percentage = _parse_bool(String(node.props["showPercentage"]), true)

	if control is Tree:
		if node.props.has("hideRoot"):
			(control as Tree).hide_root = _parse_bool(String(node.props["hideRoot"]), false)

	if control is TextureRect:
		if node.props.has("src"):
			var tex := _load_texture(_resolve_resource_uri(String(node.props["src"]), base_dir))
			if tex != null:
				(control as TextureRect).texture = tex
		if node.props.has("shrinkH") or node.props.has("shrinkV"):
			(control as TextureRect).expand_mode = TextureRect.EXPAND_IGNORE_SIZE
			(control as TextureRect).stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED

	if control is TextureButton:
		if node.props.has("textureNormal"):
			var ntex := _load_texture(_resolve_resource_uri(String(node.props["textureNormal"]), base_dir))
			if ntex != null:
				(control as TextureButton).texture_normal = ntex
		if node.props.has("texturePressed"):
			var ptex := _load_texture(_resolve_resource_uri(String(node.props["texturePressed"]), base_dir))
			if ptex != null:
				(control as TextureButton).texture_pressed = ptex
		if node.props.has("toggleMode"):
			(control as TextureButton).toggle_mode = _parse_bool(String(node.props["toggleMode"]), false)
		if node.props.has("buttonPressed"):
			(control as TextureButton).button_pressed = _parse_bool(String(node.props["buttonPressed"]), false)
		if node.props.has("textureHover"):
			var htex := _load_texture(_resolve_resource_uri(String(node.props["textureHover"]), base_dir))
			if htex != null:
				(control as TextureButton).texture_hover = htex
		if node.props.has("textureFocused"):
			var ftex := _load_texture(_resolve_resource_uri(String(node.props["textureFocused"]), base_dir))
			if ftex != null:
				(control as TextureButton).texture_focused = ftex
		if node.props.has("ignoreTextureSize"):
			(control as TextureButton).ignore_texture_size = _parse_bool(String(node.props["ignoreTextureSize"]), false)
		(control as TextureButton).stretch_mode = TextureButton.STRETCH_KEEP_ASPECT_CENTERED

	if control is OptionButton:
		if node.props.has("fitToLongestItem"):
			(control as OptionButton).fit_to_longest_item = _parse_bool(String(node.props["fitToLongestItem"]), true)

	var node_name := node.name.to_lower()
	if node_name == "dockinghost":
		control.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		control.size_flags_vertical = Control.SIZE_EXPAND_FILL
		if node.props.has("gap"):
			control.set_meta(DockingHostControl.META_DOCK_GAP, float(String(node.props["gap"])))
	elif node_name == "dockingcontainer":
		if node.props.has("fixedWidth"):
			var w: int = maxi(0, int(String(node.props["fixedWidth"])))
			control.custom_minimum_size = Vector2(float(w), control.custom_minimum_size.y)
			control.set_meta(DockingHostControl.META_DOCK_FIXED_WIDTH, float(w))
		if node.props.has("fixedHeight"):
			control.set_meta(DockingHostControl.META_DOCK_FIXED_HEIGHT, float(maxi(0, int(String(node.props["fixedHeight"])))))
		if node.props.has("heightPercent"):
			control.set_meta(DockingHostControl.META_DOCK_HEIGHT_PERCENT, float(String(node.props["heightPercent"])))
		var side := String(node.props.get("dockSide", "center")).to_lower().strip_edges()
		control.set_meta(DockingHostControl.META_DOCK_SIDE, side)
		control.set_meta(DockingHostControl.META_DOCK_FLEX, _parse_bool(String(node.props.get("flex", "false")), false))
		if side == "center":
			control.size_flags_horizontal = Control.SIZE_EXPAND_FILL
			control.size_flags_vertical = Control.SIZE_EXPAND_FILL
		else:
			control.size_flags_horizontal = Control.SIZE_SHRINK_BEGIN
			control.size_flags_vertical = Control.SIZE_EXPAND_FILL
	elif node_name == "timeline":
		control.size_flags_vertical = Control.SIZE_SHRINK_END
		if control.get_child_count() == 0 and control is VBoxContainer:
			var l := Label.new()
			l.text = "Timeline"
			control.add_child(l)

	if node.props.has("spacing") and control is BoxContainer:
		(control as BoxContainer).add_theme_constant_override("separation", int(String(node.props["spacing"])))

	if node.name.to_lower() == "posingeditor" and control is PosingEditorControl:
		var src := ""
		if node.props.has("src"):
			src = _resolve_resource_uri(String(node.props["src"]), base_dir)
		(control as PosingEditorControl).set_model_source(src)

func _resolve_docking_tab_title(child: SmlNode, docking_id: String) -> String:
	if not docking_id.is_empty():
		var key := (docking_id.to_lower() + ".title")
		for k in child.props.keys():
			if String(k).to_lower() == key:
				return _resolve_text(String(child.props[k]))
	for k in child.props.keys():
		var kk := String(k).to_lower()
		if kk.ends_with(".title"):
			return _resolve_text(String(child.props[k]))
	if child.props.has("id"):
		return String(child.props["id"])
	return child.name

func _resolve_text(raw: String) -> String:
	var t := raw.strip_edges()
	if t.begins_with("@"):
		var key := t.trim_prefix("@")
		var last := key.get_slice(".", -1)
		if _strings.has(last):
			return String(_strings[last])
		return last
	return t

func _load_strings_map(base_dir: String) -> Dictionary:
	var map: Dictionary = {}
	var path := base_dir.path_join("strings.sml")
	if not FileAccess.file_exists(path):
		return map
	var text := FileAccess.get_file_as_string(path)
	if text.is_empty():
		return map
	var regex := RegEx.new()
	if regex.compile('(?m)^\\s*([A-Za-z_][A-Za-z0-9_]*)\\s*:\\s*"([^"]*)"') != OK:
		return map
	var matches := regex.search_all(text)
	for m in matches:
		if m == null or m.get_group_count() < 2:
			continue
		map[m.get_string(1)] = m.get_string(2)
	return map

func _resolve_resource_uri(raw: String, base_dir: String) -> String:
	var value := raw.strip_edges()
	if value.begins_with("\"") and value.ends_with("\"") and value.length() >= 2:
		value = value.substr(1, value.length() - 2)
	if value.is_empty():
		return ""
	if value.begins_with("file://"):
		return _file_url_to_path(value)
	if value.find("://") >= 0:
		return value
	var appres_root := OS.get_environment("FORGE_RUNNER_APPRES_ROOT")
	if value.begins_with("appRes://"):
		if appres_root.is_empty():
			return base_dir.path_join(value.trim_prefix("appRes://"))
		return appres_root.path_join(value.trim_prefix("appRes://"))
	if value.begins_with("appRes:/"):
		if appres_root.is_empty():
			return base_dir.path_join(value.trim_prefix("appRes:/"))
		return appres_root.path_join(value.trim_prefix("appRes:/"))
	if value.begins_with("res:/"):
		if not appres_root.is_empty():
			var c := appres_root.path_join(value.trim_prefix("res:/"))
			if FileAccess.file_exists(c):
				return c
		return base_dir.path_join(value.trim_prefix("res:/"))
	return base_dir.path_join(value)

func _load_texture(uri: String) -> Texture2D:
	if uri.is_empty():
		return null
	if uri.begins_with("res://"):
		var loaded := load(uri)
		if loaded is Texture2D:
			return loaded
	if FileAccess.file_exists(uri):
		var img := Image.new()
		if img.load(uri) == OK:
			return ImageTexture.create_from_image(img)
	return null

func _parse_bool(raw: String, fallback: bool) -> bool:
	var t := raw.strip_edges().to_lower()
	if t == "true" or t == "1":
		return true
	if t == "false" or t == "0":
		return false
	return fallback

func _parse_size_tuple(raw: String) -> Vector2i:
	var parts := raw.split(",")
	if parts.size() != 2:
		return Vector2i.ZERO
	var w := int(parts[0].strip_edges())
	var h := int(parts[1].strip_edges())
	return Vector2i(w, h)

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

# -----------------------------
# Minimal SML parser
# -----------------------------

func _parse_sml(source: String) -> SmlNode:
	_src = source
	_pos = 0
	_skip_ws_comments()
	return _parse_node()

func _parse_node() -> SmlNode:
	var node_name := _parse_identifier()
	if node_name.is_empty():
		return null
	_skip_ws_comments()
	if _peek() != "{":
		return null
	_pos += 1
	var node := SmlNode.new()
	node.name = node_name

	while _pos < _src.length():
		_skip_ws_comments()
		if _pos >= _src.length():
			break
		if _peek() == "}":
			_pos += 1
			break
		var entry_start := _pos
		var entry_name := _parse_identifier()
		if entry_name.is_empty():
			_pos += 1
			continue
		_skip_ws_comments()
		if _peek() == ":":
			_pos += 1
			node.props[entry_name] = _parse_property_value_line()
			continue
		if _peek() == "{":
			_pos = entry_start
			var child := _parse_node()
			if child != null:
				node.children.append(child)
				continue
		_skip_to_line_end()

	return node

func _parse_identifier() -> String:
	_skip_ws_comments()
	var out := ""
	while _pos < _src.length():
		var ch := _src[_pos]
		var ok := _is_ident_char(ch)
		if ok:
			out += ch
			_pos += 1
		else:
			break
	return out

func _parse_property_value_line() -> String:
	_skip_ws_comments()
	var out := ""
	var in_string := false
	var escaping := false
	while _pos < _src.length():
		var ch := _src[_pos]
		if in_string:
			out += ch
			_pos += 1
			if escaping:
				escaping = false
				continue
			if ch == "\\":
				escaping = true
				continue
			if ch == '"':
				in_string = false
			continue
		if ch == '"':
			in_string = true
			out += ch
			_pos += 1
			continue
		if ch == "\n" or ch == "\r" or ch == "}":
			break
		# stop before next inline property pattern " ident :"
		if ch == " " or ch == "\t":
			var la := _pos
			while la < _src.length() and (_src[la] == " " or _src[la] == "\t"):
				la += 1
			if la < _src.length() and _is_ident_start_char(_src[la]):
				var id_end := la + 1
				while id_end < _src.length() and _is_ident_char(_src[id_end]):
					id_end += 1
				if id_end < _src.length() and _src[id_end] == ":":
					break
		out += ch
		_pos += 1
	while _pos < _src.length() and (_src[_pos] == "\n" or _src[_pos] == "\r"):
		_pos += 1
	return out.strip_edges().trim_prefix("\"").trim_suffix("\"")

func _skip_ws_comments() -> void:
	while _pos < _src.length():
		var ch := _src[_pos]
		if ch == " " or ch == "\t" or ch == "\n" or ch == "\r":
			_pos += 1
			continue
		if ch == "/" and _pos + 1 < _src.length() and _src[_pos + 1] == "/":
			_pos += 2
			while _pos < _src.length() and _src[_pos] != "\n":
				_pos += 1
			continue
		if ch == "/" and _pos + 1 < _src.length() and _src[_pos + 1] == "*":
			_pos += 2
			while _pos + 1 < _src.length() and not (_src[_pos] == "*" and _src[_pos + 1] == "/"):
				_pos += 1
			if _pos + 1 < _src.length():
				_pos += 2
			continue
		break

func _skip_to_line_end() -> void:
	while _pos < _src.length() and _src[_pos] != "\n" and _src[_pos] != "\r":
		_pos += 1
	while _pos < _src.length() and (_src[_pos] == "\n" or _src[_pos] == "\r"):
		_pos += 1

func _peek() -> String:
	if _pos >= _src.length():
		return ""
	return _src[_pos]

func _is_ident_start_char(ch: String) -> bool:
	return (ch >= "a" and ch <= "z") or (ch >= "A" and ch <= "Z") or ch == "_"

func _is_ident_char(ch: String) -> bool:
	return _is_ident_start_char(ch) or (ch >= "0" and ch <= "9") or ch == "."

func _match_string(text: String, pattern: String) -> String:
	var regex := RegEx.new()
	var err := regex.compile(pattern)
	if err != OK:
		return ""
	var result := regex.search(text)
	if result == null or result.get_group_count() < 1:
		return ""
	return result.get_string(1)
