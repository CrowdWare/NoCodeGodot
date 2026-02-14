# SMS API für Docking und MainMenu

Diese Seite dokumentiert die Funktionen, die in einem **UserDefined Script `*.sms`** für

- **MainMenu/MenuBar**
- **Docking (`DockSpace`, `DockPanel`)**

verwendet werden können.

Die gezeigten APIs werden vom Runtime-Host (`SmsUiRuntime`) bereitgestellt.

---

## 1) Einstiegspunkte (Callbacks) in SMS

Folgende Funktionen kannst du im `app.sms` definieren. Sie werden vom Runner aufgerufen, wenn vorhanden.

### `fun ready()`

Wird nach dem Laden des UI-Skripts aufgerufen.

Typischer Einsatz:

- UI-Objekte über `getObject(id)` holen
- Event-Bindings setzen
- Initialdaten laden

---

### `fun menuItemSelected(menu, item)`

Wird bei Klick auf ein `MenuItem` ausgelöst.

- `menu`: ID des Menüs (z. B. `appMenu`)
- `item`: ID des geklickten Menüeintrags (z. B. `saveFile`)

Beispiel:

```sms
fun menuItemSelected(menu, item) {
    log.info("menuItemSelected -> menu=${menu}, item=${item}")
}
```

---

### `fun treeItemSelected(treeView, itemText, itemPath)`

Wird bei Auswahl in einem `TreeView` ausgelöst.

- `treeView`: TreeView-ID
- `itemText`: sichtbarer Text des Items
- `itemPath`: Metadaten-Pfad des Items (falls gesetzt)

---

### `fun treeItemToggled(treeView, itemText, itemPath, isOn)`

Wird bei Toggle-Interaktion im TreeView ausgelöst.

- `isOn`: `true`/`false`

---

### `fun <deinCallback>(editorId)` via `codeEdit.onSave(...)`

`CodeEdit` kann einen Save-Callback registrieren, der bei Save-Aktion ausgeführt wird.

```sms
var codeEdit = null

fun ready() {
    codeEdit = getObject("codeEdit")
    if (codeEdit != null) {
        codeEdit.onSave("codeEditOnSave")
    }
}

fun codeEditOnSave(editorId) {
    log.success("Save callback für ${editorId}")
}
```

---

## 2) Globale SMS-Objekte

Diese Objekte sind standardmäßig verfügbar:

## `fs`

- `fs.readText(path)`
- `fs.writeText(path, text)`
- `fs.exists(path)`
- `fs.list(dir)`

## `log`

- `log.info(message)`
- `log.warn(message)`
- `log.error(message)`
- `log.success(message)`

---

## 3) UI-Objekte per `getObject(id)`

Mit `getObject(id)` kannst du UI-Knoten nach SML-ID holen.

Unterstützte Objekttypen:

- `TreeView`
- `CodeEdit`
- `DockSpace`
- `Menu`/`MenuButton` (für dynamische Menüeinträge)

Beide Aufrufarten sind möglich:

```sms
var ds1 = getObject("editorDock")
var ds2 = getObject(editorDock)
```

`getObject(editorDock)` funktioniert, weil UI-IDs als SMS-Globale bereitgestellt werden (sofern die ID ein gültiger SMS-Identifier ist).

---

## 4) DockSpace API (neu für SMS)

Wenn `id` auf einen `DockSpace` zeigt, liefert `getObject(id)` ein DockSpace-Objekt mit:

- `SaveLayout(path)` → `bool`
- `LoadLayout(path)` → `bool`
- `ResetLayout()` → `null`
- `GetClosePanelList()` → `array<string>`
- `GetClosedPanelList()` → `array<string>` (Alias)
- `ReopenPanel(panelId)` → `bool`

### Event: `fun dockPanelClosed(dockSpace, panelName)`

Wird aufgerufen, wenn ein DockPanel geschlossen wurde.

- `dockSpace`: ID des DockSpace
- `panelName`: Panel-ID

### `SaveLayout(path)`

Speichert das aktuelle Docking-Layout.

- Gibt `true` bei Erfolg zurück, sonst `false`.
- `path` darf sein:
  - absoluter Pfad
  - `user://...`
  - `user:/...` (wird automatisch zu `user://...` normalisiert)
  - `res://...`
  - `res:/...` (wird automatisch zu `res://...` normalisiert)
  - relativer Name (wird als `user://<name>` behandelt)

### `LoadLayout(path)`

Lädt ein gespeichertes Layout.

- Gibt `true` bei Erfolg zurück, sonst `false`.
- Gleiche Pfadregeln wie bei `SaveLayout`.

### `ResetLayout()`

Setzt auf den Default-Snapshot zurück (initiales Dock-Layout nach UI-Aufbau).

### `GetClosePanelList()` / `GetClosedPanelList()`

Liefert die aktuell geschlossenen Panel-IDs als Array von Strings zurück.

### `ReopenPanel(panelId)`

Öffnet ein zuvor geschlossenes Panel wieder.

- Rückgabe: `true` bei Erfolg, sonst `false`.

---

## 5) Menu API (dynamische Einträge)

Wenn `getObject(id)` ein Menü liefert, ist verfügbar:

- `AddMenuItem(itemId, popupId)`

Beispiel:

```sms
var view = getObject("viewMenu")
view.AddMenuItem("panelx", 66)
```

Beim Klick auf den dynamisch hinzugefügten Eintrag wird normal
`menuItemSelected(menu, item)` ausgelöst (`menu=viewMenu`, `item=panelx`).

---

## 6) Komplettes Beispiel: MainMenu + DockLayout + Reopen

```sms
var dock = null
var view = null
var nextDynamicMenuId = 100

fun ready() {
    dock = getObject(editorDock)
    view = getObject("viewMenu")
}

fun dockPanelClosed(dockSpace, panelName) {
    if (view != null) {
        view.AddMenuItem(panelName, nextDynamicMenuId)
        nextDynamicMenuId = nextDynamicMenuId + 1
    }
}

fun menuItemSelected(menu, item) {
    if (dock == null) {
        return
    }

    if (item == "saveFile") {
        var ok = dock.SaveLayout("designer_layout.json")
        log.info("SaveLayout: ${ok}")
    }
    else if (item == "openFile") {
        var ok = dock.LoadLayout("designer_layout.json")
        log.info("LoadLayout: ${ok}")
    }
    else if (item == "settings") {
        dock.ResetLayout()
        log.info("Layout zurückgesetzt")
    }
    else {
        // Versuch, ein geschlossenes Panel über seinen Menüeintrag wieder zu öffnen
        var reopened = dock.ReopenPanel(item)
        if (reopened) {
            log.info("Panel wieder geöffnet: ${item}")
        }
    }
}
```

Hinweis: Für dynamische Menüeinträge ist eine eigene laufende ID (Counter) sinnvoll, damit jeder Eintrag eine eindeutige Popup-ID erhält.

---

## 7) Hinweise zu Docking vs. SMS

- **Docking-Struktur** (`area`, `closeable`, `floatable`, `dockable`, `isDropTarget`, `allowSplitting` usw.) wird in **SML** definiert.
- **SMS** reagiert auf Events und steuert Laufzeitaktionen (z. B. Layout laden/speichern, Dateiinhalte im Dock-Panel anzeigen).
