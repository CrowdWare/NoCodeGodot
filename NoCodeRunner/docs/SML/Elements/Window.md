# Window

## Inheritance

[Window](Window.md) → [Viewport](Viewport.md) → [Node](Node.md) → [Object](Object.md)

## Derived Classes

### Direct subclasses

- [AcceptDialog](AcceptDialog.md)
- [Popup](Popup.md)

## Properties

This page lists **only properties declared by `Window`**.
Inherited properties are documented in: [Viewport](Viewport.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| accessibility_description | accessibilityDescription | string | — |
| accessibility_name | accessibilityName | string | — |
| always_on_top | alwaysOnTop | bool | — |
| borderless | borderless | bool | — |
| content_scale_aspect | contentScaleAspect | int | — |
| content_scale_factor | contentScaleFactor | float | — |
| content_scale_mode | contentScaleMode | int | — |
| content_scale_stretch | contentScaleStretch | int | — |
| current_screen | currentScreen | int | — |
| exclude_from_capture | excludeFromCapture | bool | — |
| exclusive | exclusive | bool | — |
| extend_to_title | extendToTitle | bool | — |
| force_native | forceNative | bool | — |
| initial_position | initialPosition | int | — |
| keep_title_visible | keepTitleVisible | bool | — |
| maximize_disabled | maximizeDisabled | bool | — |
| minimize_disabled | minimizeDisabled | bool | — |
| mode | mode | int | — |
| mouse_passthrough | mousePassthrough | bool | — |
| popup_window | popupWindow | bool | — |
| popup_wm_hint | popupWmHint | bool | — |
| sharp_corners | sharpCorners | bool | — |
| theme_type_variation | themeTypeVariation | string | — |
| title | title | string | — |
| transient | transient | bool | — |
| transient_to_focused | transientToFocused | bool | — |
| transparent | transparent | bool | — |
| unfocusable | unfocusable | bool | — |
| unresizable | unresizable | bool | — |
| visible | visible | bool | — |
| wrap_controls | wrapControls | bool | — |

## Events

This page lists **only signals declared by `Window`**.
Inherited signals are documented in: [Viewport](Viewport.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| about_to_popup | `on <id>.aboutToPopup() { ... }` | — |
| close_requested | `on <id>.closeRequested() { ... }` | — |
| dpi_changed | `on <id>.dpiChanged() { ... }` | — |
| files_dropped | `on <id>.filesDropped(files) { ... }` | Variant files |
| focus_entered | `on <id>.focusEntered() { ... }` | — |
| focus_exited | `on <id>.focusExited() { ... }` | — |
| go_back_requested | `on <id>.goBackRequested() { ... }` | — |
| mouse_entered | `on <id>.mouseEntered() { ... }` | — |
| mouse_exited | `on <id>.mouseExited() { ... }` | — |
| nonclient_window_input | `on <id>.nonclientWindowInput(event) { ... }` | Object event |
| theme_changed | `on <id>.themeChanged() { ... }` | — |
| title_changed | `on <id>.titleChanged() { ... }` | — |
| titlebar_changed | `on <id>.titlebarChanged() { ... }` | — |
| visibility_changed | `on <id>.visibilityChanged() { ... }` | — |
| window_input | `on <id>.windowInput(event) { ... }` | Object event |
