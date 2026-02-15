# OptionButton

## Inheritance

[OptionButton](OptionButton.md) → [Button](Button.md) → [BaseButton](BaseButton.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Derived Classes

### Direct subclasses

- [EditorVariantTypeOptionButton](EditorVariantTypeOptionButton.md)

## Properties

This page lists **only properties declared by `OptionButton`**.
Inherited properties are documented in: [Button](Button.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| allow_reselect | allowReselect | bool | — |
| fit_to_longest_item | fitToLongestItem | bool | — |
| item_count | itemCount | int | — |
| selected | selected | int | — |

## Events

This page lists **only signals declared by `OptionButton`**.
Inherited signals are documented in: [Button](Button.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| item_focused | `on <id>.itemFocused(index) { ... }` | int index |
| item_selected | `on <id>.itemSelected(index) { ... }` | int index |

## SML Items

`OptionButton` options are defined as **SML child nodes** (pseudo elements).
The runtime converts them to Godot OptionButton items internally.

### Supported item elements

- `Item`

### Item properties (SML)

| Property | Type | Default | Notes |
|-|-|-|-|
| id | identifier | — | Optional. Enables id-based event sugar (`on <id>.selected() { ... }`). |
| text | string | "" | Display text. |
| icon | string | "" | Optional icon resource/path. |
| disabled | bool | false | Disables the option. |
| selected | bool | false | If true, selects this option initially (first wins). |

### Example

```sml
OptionButton { id: quality
    Item { id: low; text: "Low" }
    Item { id: med; text: "Medium"; selected: true }
    Item { id: high; text: "High" }
}
```

### SMS Event Examples

```sms
// With explicit item ids:
on med.selected() { ... }

// Container fallback (index based):
on quality.itemSelected(index) { ... }
```
