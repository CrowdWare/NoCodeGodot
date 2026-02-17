# SMS Runtime Functions

This document lists built-in SMS runtime helper functions available in NoCode.

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
Returns a reference to an object by id or name.

### ui.CreateWindow(smlText: String)
Creates a floating window from inline SML text and returns a Window instance (or null on failure). The returned Window supports runtime actions onClose(callbackName) and close().

### ui.createDialog(path: String)
Creates a dialog from either a resource path (res:/...) or an inline SML definition string and returns the dialog instance.

