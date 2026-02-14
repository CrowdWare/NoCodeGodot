# SMS API für Docking und MainMenu

Diese Seite dokumentiert die Funktionen, die in einem **UserDefined Script (`*.sms`)** für

- **MainMenu / MenuBar**
- **Docking (`DockSpace`, `DockPanel`)**

verwendet werden können.

---

## 1) Einstiegspunkte (Callbacks)

### `fun ready()`
Wird nach dem Laden des UI aufgerufen.

### `fun menuItemSelected(menu, item)`
Wird bei Klick auf ein Menü-Item ausgelöst.

- `menu`: Menü-ID (z. B. `viewMenu`)
- `item`: **MenuItem-Objekt**

`item` hat:

- `item.id`
- `item.text`
- `item.isChecked`
- `item.menuId`
- `item.SetChecked(bool)`
- `item.SetText(string)`
- `item.GetText()`
- `item.IsChecked()`

### `fun dockPanelClosed(dockSpace, panelName)`
Wird ausgelöst, wenn ein DockPanel geschlossen wurde.

- `dockSpace`: DockSpace-ID
- `panelName`: Panel-ID

### `fun treeItemSelected(treeView, itemText, itemPath)` / `fun treeItemToggled(...)`
TreeView-Events wie bisher.

---

## 2) `getObject(id)` – unterstützte Objekte

- `TreeView`
- `CodeEdit`
- `DockSpace`
- `Menu` / `MenuButton`
- `MenuItem` (statisch definierte IDs und dynamisch hinzugefügte Einträge)

---

## 3) DockSpace API

Wenn `getObject(id)` ein DockSpace liefert:

- `SaveLayout(path)` → `bool`
- `LoadLayout(path)` → `bool`
- `ResetLayout()`
- `GetClosePanelList()` → `array<string>`
- `GetClosedPanelList()` → `array<string>` (Alias)
- `ReopenPanel(panelId)` → `bool`

Pfadregeln für Save/Load:

- absoluter Pfad
- `user://...`
- `user:/...` (wird zu `user://...` normalisiert)
- `res://...`
- `res:/...` (wird zu `res://...` normalisiert)
- relativer Name (`user://<name>`)

---

## 4) Menu / MenuItem API

### Menu (`getObject("viewMenu")`)
- `AddMenuItem(itemId, popupId)`

### MenuItem (`getObject("panelRight")`)
- `SetChecked(bool)`
- `SetText(string)`
- Properties: `id`, `text`, `isChecked`, `menuId`

Beispiel:

```sms
var panelRight = getObject("panelRight")
panelRight.SetChecked(false)
panelRight.SetText("Markdown (geschlossen)")
```

---

## 5) Beispiel: statisches View-Menü mit Check-Status

```sms
var dock = null
var panelLeftMenuItem = null
var panelRightMenuItem = null

fun ready() {
    dock = getObject("editorDock")
    panelLeftMenuItem = getObject("panelLeft")
    panelRightMenuItem = getObject("panelRight")
    syncPanelChecksFromDockState()
}

fun dockPanelClosed(dockSpace, panelName) {
    var menuItemId = mapPanelIdToMenuItemId(panelName)
    if (menuItemId != "") {
        var item = getObject(menuItemId)
        if (item != null) {
            item.SetChecked(false)
        }
    }
}

fun menuItemSelected(menu, item) {
    if (item == null || dock == null) {
        return
    }

    var itemId = item.id

    if (itemId == "saveFile") {
        dock.SaveLayout("designer_layout.json")
    }
    else if (itemId == "openFile") {
        dock.LoadLayout("designer_layout.json")
        syncPanelChecksFromDockState()
    }
    else if (itemId == "settings") {
        dock.ResetLayout()
        syncPanelChecksFromDockState()
    }
    else {
        var panelId = mapMenuItemIdToPanelId(itemId)
        if (panelId != "") {
            var reopened = dock.ReopenPanel(panelId)
            if (reopened) {
                item.SetChecked(true)
            }
        }
    }
}

fun mapPanelIdToMenuItemId(panelId) {
    if (panelId == "leftTop") { return "panelLeft" }
    if (panelId == "rightTop") { return "panelRight" }
    return ""
}

fun mapMenuItemIdToPanelId(menuItemId) {
    if (menuItemId == "panelLeft") { return "leftTop" }
    if (menuItemId == "panelRight") { return "rightTop" }
    return ""
}

fun syncPanelChecksFromDockState() {
    if (dock == null) {
        return
    }

    if (panelLeftMenuItem != null) { panelLeftMenuItem.SetChecked(true) }
    if (panelRightMenuItem != null) { panelRightMenuItem.SetChecked(true) }

    var closed = dock.GetClosedPanelList()
    for (panelId in closed) {
        var menuItemId = mapPanelIdToMenuItemId(panelId)
        if (menuItemId != "") {
            var item = getObject(menuItemId)
            if (item != null) {
                item.SetChecked(false)
            }
        }
    }
}
```
