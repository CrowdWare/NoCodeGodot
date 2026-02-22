# SMS Runtime Functions

This document lists built-in SMS runtime helper functions available in Forge.

## File System

### fs.list(path: String)
Lists files in a directory and returns an array of file names.

### fs.readText(path: String)
Reads text content from a file and returns it as a String.

### fs.writeText(path: String, content: String)
Writes text content to a file.

## Logging

### log.error(message: String)
Logs an error message.

### log.info(message: String)
Logs an informational message.

### log.success(message: String)
Logs a success message.

### log.warn(message: String)
Logs a warning message.

## UI

### ui.getObject(name: String)
Returns a runtime object for any SML element with a matching id. Exposes callable methods dynamically from the underlying Godot object. Returns null when no matching id exists.

### ui.CreateWindow(smlText: String)
Creates a floating window from inline SML text and returns a Window instance (or null on failure). The returned Window supports runtime actions onClose(callbackName) and close().

### ui.createDialog(path: String)
Creates a dialog from either a resource path (res:/...) or an inline SML definition string and returns the dialog instance.

### Window Flag Constants
Enum-like global constants for Window flags in SMS. Use with setFlag/getFlag without magic numbers, e.g. mainWindow.setFlag(extendToTitle, true). Available constants: borderless=0, alwaysOnTop=1, transparent=2, noFocus=3, popup=5, extendToTitle=6, mousePassthrough=7, sharpCorners=8, excludeFromCapture=9, popupWmHint=10, minSize=11, maxSize=12, resizeDisabled=13, transient=14, modal=15, popupExclusive=16.

### <windowId>.getFlag(flag: int)
Gets a Window flag value by enum constant (recommended) or integer value. Example: var on = mainWindow.getFlag(extendToTitle).

### <windowId>.setFlag(flag: int, enabled: bool)
Sets a Window flag by enum constant (recommended) or integer value. Example: mainWindow.setFlag(extendToTitle, true).

