# SMS Cookbook v0

Praktische SMS-Muster statt abstrakter Grammatik.

## 1) Event Handler

Gültig:

```sms
on menuSave.clicked() {
    saveProject()
}
```

Ungültig:

```sms
on MenuSave.clicked() { }   // Ziel-ID soll lowerCamelCase sein
```

Warum: Event-Targets bleiben konsistent und besser auffindbar.

## 2) Event mit Parametern

Gültig:

```sms
on treeview.itemSelected(path: String) {
    log.info("selected: ${path}")
}
```

Warum: Typen helfen Tooling und späterem Native-Backend.

## 3) Funktion

Gültig:

```sms
fun add(a: Int32, b: Int32): Int32 {
    return a + b
}
```

Warum: Klare Signaturen sind AI- und Team-freundlich.

## 4) Variable

Gültig:

```sms
var isDirty: Bool = false
```

Ungültig:

```sms
var isDirty: Int23 = false
```

Warum: Unbekannte Typen sollen früh auffallen.

## 5) Module Import

Gültig:

```sms
import "./math.sms" as math

fun ready() {
    var x: Int32 = math.add(1, 2)
}
```

Warum: Explizite Imports statt versteckter Global-Magie.

## 6) Export

Gültig:

```sms
export fun add(a: Int32, b: Int32): Int32 {
    return a + b
}
```

Warum: Reuse wird sichtbar und kontrollierbar.

## 7) If / Else

Gültig:

```sms
if (isDirty) {
    saveProject()
} else {
    log.info("nothing to save")
}
```

Warum: Explizite Blöcke sind robust und lesbar.

## 8) When

Gültig:

```sms
fun modeLabel(mode: String): String {
    when (mode) {
        "pose" -> return "Pose Mode"
        "move" -> return "Move Mode"
        else -> return "Unknown"
    }
}
```

Warum: Klarer als lange if-else-Ketten.

## 9) For Loop

Gültig:

```sms
var sum: Int64 = 0
for (var i: Int32 = 0; i < 100; i = i + 1) {
    sum = sum + i
}
```

Warum: Deterministisches, einfaches Loop-Modell.

## 9b) For-In über Liste

Gültig:

```sms
var list = [ "tree", "rock", "chair" ]
for (item in list) {
    log.info(item)
}
```

Warum: Für typische UI-/Game-Listen oft lesbarer als indexbasierte Schleifen.

## 10) Named Args (nur named)

Gültig:

```sms
var vec = Vec3D(y = 100)
```

Warum: Defaults bleiben aktiv, relevante Felder sind klar.

## 11) Named Args (Mischform)

Gültig:

```sms
var vec = Vec3D(10, y = 100)
```

Ungültig:

```sms
var vec = Vec3D(y = 100, 10)
```

Warum: Erst positional, danach named. Nie umgekehrt.

## 12) Named Args Fehlerfälle

Ungültig:

```sms
Vec3D(y = 100, y = 5)       // duplicate arg
Vec3D(speed = 10)           // unknown arg
```

Warum: Doppelte oder unbekannte Namen sind immer Fehler.

## 13) Super im Event

Gültig:

```sms
on mainWindow.sizeChanged(w: Int32, h: Int32) {
    layout.update(w, h)
    super(w, h)
}
```

Warum: Erweitern statt verdeckt ersetzen.

## 14) Async Denkweise (v0 Promise)

Regel:

```sms
// await darf keinen Scope "verschwinden" lassen
```

Warum: Devs sollen sich nie fragen müssen, wo ihre Variablen geblieben sind.

## 15) Logging

Gültig:

```sms
log.success("Saved")
log.info("Ready")
log.debug("internal detail")
```

Warum: User-relevante Messages und Debug-Rauschen getrennt halten.

## 16) Kleine Checkliste für neue SMS-Dateien

1. Sind Event-Handler klar benannt (`on id.event`)?
2. Sind Typen dort gesetzt, wo sie Klarheit bringen?
3. Sind Named-Args regelkonform (positional zuerst)?
4. Gibt es nur notwendige Logs in `info/success`?
5. Ist der Code ohne Runtime-Magie lesbar?
