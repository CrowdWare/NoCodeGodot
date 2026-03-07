# Native SMS ↔ Godot UI Bridge

## Goal
Replace the `ui_get_stub` / `ui_set_stub` placeholder functions in `ForgeRunner.Native`
with a real bidirectional bridge between the SMS interpreter and the Godot Control tree.

## Context
`ForgeRunner.Native/src/main.cpp` wires SMS via `set_ui_callbacks_fn_`, but the
registered callbacks (`ui_get_stub`, `ui_set_stub`) are stubs that do nothing.
The C# equivalent is `SmsUiRuntime.cs` + `UiRuntimeApi.cs`.

The `forge_runner_extension.cpp` already has a working node registry (via
`ForgeDockingHostControl`, etc.). The bridge needs a global object-id → Control*
map so SMS can look up controls by SML id.

## Subsystems to Port

### Object registry
- `id_map_: std::unordered_map<std::string, godot::Control*>` populated during
  `UiBuilder::build_node()` when an `id:` property is present.
- Cleared + invalidated on UI reload.

### ui_get (property read)
Translate `(object_id, property_name)` → JSON value string.

| SMS property | Godot getter |
|---|---|
| `text` | `Label::get_text()`, `Button::get_text()`, `LineEdit::get_text()` |
| `value` | `SpinBox::get_value()`, `Slider::get_value()` |
| `checked` / `buttonPressed` | `CheckBox::is_pressed()` |
| `visible` | `Control::is_visible()` |
| `disabled` | `Control::is_disabled()` |
| `selectedIndex` | `OptionButton::get_selected()` |
| `selectedText` | `OptionButton::get_item_text(get_selected())` |
| `caretColumn` | `LineEdit::get_caret_column()` |
| `scrollV` | `ScrollContainer::get_v_scroll()` |

### ui_set (property write)
Reverse mapping. Include `addItem`, `clearItems` for OptionButton / ItemList.

### ui_invoke (method call)
Support at minimum:
- `focus()` → `grab_focus()`
- `scrollToBottom()` → `ScrollContainer::set_v_scroll(INT_MAX)`
- `clearItems()` → `OptionButton::clear()` / `ItemList::clear()`
- `addItem(text)` → `OptionButton::add_item()` / `ItemList::add_item()`

### Event dispatch (→ SMS)
When Godot signals fire (button pressed, text changed, value changed, etc.),
call `sms_invoke_event(session_id, object_id, event_name, payload_json)`.
Wire in `forge_ui_builder.cpp` after build, mirroring `SmlUiBuilder.BindInteractions`.

## Reference
- C#: `SmsUiRuntime.cs`, `UiRuntimeApi.cs`, `SmlUiBuilder.BindInteractions()`
- C++ stubs: `ForgeRunner.Native/src/main.cpp` → `ui_get_stub`, `ui_set_stub`
