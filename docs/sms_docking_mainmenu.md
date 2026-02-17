# SMS API für Docking und MainMenu

Diese Seite dokumentiert die Funktionen, die in einem **UserDefined Script (`*.sms`)** für

- **MainMenu / MenuBar**
- **Docking (`DockSpace`, `DockPanel`)**

verwendet werden können.

---

## 1) Einstiegspunkte (Callbacks)

### `fun ready()`
Wird nach dem Laden des UI aufgerufen.

### `on <menuItemId>.clicked()`
Wird bei Klick auf ein Menü-Item ausgelöst (event-first).

Beispiele:

```sms
on saveFile.clicked() { ... }
on panelRight.clicked() { ... }
```

### `fun dockPanelClosed(dockSpace, panelName)`
Wird ausgelöst, wenn ein DockPanel geschlossen wurde.

- `dockSpace`: DockSpace-ID
- `panelName`: Panel-ID

### `fun treeItemSelected(treeView, itemText, itemPath)` / `fun treeItemToggled(...)`
TreeView-Events wie bisher.

---

## 2) `ui.getObject(id)` – unterstützte Objekte

- `TreeView`
- `CodeEdit`
- `DockSpace`
- `Menu` / `MenuButton`
- `MenuItem` (statisch definierte IDs und dynamisch hinzugefügte Einträge)

---

## 3) DockSpace API

Wenn `ui.getObject(id)` ein DockSpace liefert:

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

### Menu (`ui.getObject("viewMenu")`)
- `AddMenuItem(itemId, popupId)`

### MenuItem (`ui.getObject("panelRight")`)
- `SetChecked(bool)`
- `SetText(string)`
- Properties: `id`, `text`, `isChecked`, `menuId`

Zusätzlich kann der Initialzustand deklarativ in SML gesetzt werden:

```sml
MenuItem { text: "Markdown" id: panelRight isChecked: true }
```

Beispiel:

```sms
var panelRight = ui.getObject("panelRight")
panelRight.SetChecked(false)
panelRight.SetText("Markdown (geschlossen)")
```

---

## 5) `ui.CreateWindow(sml)` – schwebendes Fenster aus SMS

Über das globale Objekt `ui` kann aus SMS zur Laufzeit ein freistehendes Fenster erzeugt werden.

### Signatur

- `ui.CreateWindow(smlText)` → `Window | null`
- `window.onClose(callbackName)` → registriert instanzgebundenen Close-Callback
- `window.close()` → schließt die Fenster-Instanz

### Verhalten

- erstellt ein **eigenes natives Fenster** (Godot `Window`)
- setzt **AlwaysOnTop = true**
- ist **freistehend** (`Transient = false`), kann also über der Anwendung schweben oder auf einen anderen Bildschirm verschoben werden
- wird mit der Anwendung beendet (normaler App-Lifecycle)
- beim manuellen Schließen wird der für diese Instanz gesetzte `onClose`-Callback aufgerufen (falls gesetzt)

### Beispiel

```sms
var win = ui.CreateWindow("Window {\n"
    + "  id: mainWindow\n"
    + "  title: \"NoCodeDesigner\"\n"
    + "  minSize: 800,400\n"
    + "  pos: 448,156\n"
    + "  size: 1024,768\n"
    + "  Page {\n"
    + "    Label { text: \"Floating Window\" }\n"
    + "  }\n"
    + "}")

if (win != null) {
    win.onClose("onFloatingWindowClosed")
}

fun onFloatingWindowClosed() {
    log.info("Floating window wurde geschlossen")
}
```

---

## 6) Beispiel: statisches View-Menü mit Check-Status

```sms
var dock = null

fun ready() {
    dock = ui.getObject("editorDock")
}

fun dockPanelClosed(dockSpace, panelName) {
    var menuItemId = mapPanelIdToMenuItemId(panelName)
    if (menuItemId != "") {
        var item = ui.getObject(menuItemId)
        if (item != null) {
            item.SetChecked(false)
        }
    }
}

on saveFile.clicked() {
    if (dock == null) { return }
    dock.SaveLayout("designer_layout.json")
}

on openFile.clicked() {
    if (dock == null) { return }
    dock.LoadLayout("designer_layout.json")
}

on settings.clicked() {
    if (dock == null) { return }
    dock.ResetLayout()
}

on panelLeft.clicked() {
    reopenPanelFromMenuItem("panelLeft")
}

on panelRight.clicked() {
    reopenPanelFromMenuItem("panelRight")
}

fun reopenPanelFromMenuItem(menuItemId) {
    if (dock == null) { return }

    var panelId = mapMenuItemIdToPanelId(menuItemId)
    if (panelId == "") { return }

    var item = ui.getObject(menuItemId)
    if (item == null) { return }

    var reopened = dock.ReopenPanel(panelId)
    if (reopened) {
        item.SetChecked(true)
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
```
