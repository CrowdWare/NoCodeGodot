# CanvasItem

## Godot Mapping
Godot Node: CanvasItem

## Properties
| Name | Type | Default | Description |
|-|-|-|-|
| id | identifier | — | Unique element id used for SMS event binding |
| visible | bool | true | Visibility of the item |
| modulate | color | 1,1,1,1 | Modulation color applied to the item |
| zIndex | int | 0 | Draw order within the same parent |
| position | Vector2 | 0,0 | Local position |
| rotation | float | 0 | Local rotation in radians |
| scale | Vector2 | 1,1 | Local scale |

## Events
| Event | Params | Description |
|-|-|-|
| visibilityChanged | — | Emitted when visibility changes |

## Example

```sml
CanvasItem {
    id: example
}
```

## SMS Event Examples

```sms
on example.visibilityChanged() {
    log.info("Visibility changed")
}
```
