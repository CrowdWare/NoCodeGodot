# Button

## Godot Mapping
Godot Node: Button

## Properties
| Name | Type | Default | Description |
|-|-|-|-|
| id | identifier | — | Unique element id used for SMS event binding |
| text | string | "" | Button caption |
| disabled | bool | false | Disables user interaction |
| toggleMode | bool | false | Button acts as a toggle button |
| pressed | bool | false | Current pressed/toggled state |
| icon | resource | — | Optional button icon texture |
| flat | bool | false | Removes background styling |

## Events
| Event | Params | Description |
|-|-|-|
| pressed | — | Button was pressed |
| toggled | bool pressed | Emitted when toggleMode changes state |

## Example

```sml
Button {
    id: saveButton
    text: "Save"
    disabled: false      // default
    toggleMode: false    // default
    pressed: false       // default
    flat: false          // default
}
```

## SMS Event Examples

```sms
on saveButton.pressed() {
    log.info("Button clicked")
}

on saveButton.toggled(pressed) {
    log.info("Toggle state: ", pressed)
}
```