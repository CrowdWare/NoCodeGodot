# Window

## Godot Mapping
Godot Node: Window

## Properties
### Common Window Properties

| Name | Type | Alternative | Description |
|-|-|-|-|
| id | identifier | — | Unique element id used for SMS event binding |
| title | string | — | Window title |
| minSize | Vector2 | minWidth + minHeight | Minimum window size |
| size | Vector2 | width + height | Window size |
| pos | Vector2 | x + y | Window position |
| designSize | Vector2 | designWidth + designHeight | Required when `scaling: fixed`; used as fixed render design resolution|
| scaling | enum | — | `layout` or `fixed`|



## Events
(To be generated)

## Example

```sml
Window {
    id: mainWindow
    title: "MyAppname"
    pos: 20, 20
    size: 1024, 768
    scaling: layout // default
}
```
