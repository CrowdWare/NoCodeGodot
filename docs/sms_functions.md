# SMS Runtime Functions

This document lists built-in SMS runtime helper functions available in Forge.

## File System

### fs.list(path: String)
Lists files in a directory and returns an array of file names.

### fs.readText(path: String)
Reads text content from a file and returns it as a String.

### fs.writeText(path: String, content: String)
Writes text content to a file.

## Internationalization

### i18n.getLocale()
Returns the active two-letter ISO language code (for example: en, de, es).

### i18n.setLocale(locale: String)
Switches the active locale and reloads the corresponding strings-<locale>.sml file. Subsequent i18n.tr() calls return translations for the new locale. Controls whose text was already rendered during UI build are not automatically updated; update them explicitly via script after calling setLocale. The call is a no-op when the requested locale is already active. A warning is logged on load failure and the previous locale is retained.

### i18n.tr(key: String, default: String?)
Returns the translation for the given key in the current locale. Falls back to the default value when provided, or to the key itself when no default is given and no translation is found.

## Logging

### log.error(message: String)
Logs an error message.

### log.info(message: String)
Logs an informational message.

### log.success(message: String)
Logs a success message.

### log.warn(message: String)
Logs a warning message.

## OS

### os.getArch()
Returns the current process architecture (for example: arm64 or x64).

### os.getCountry()
Returns the current two-letter ISO country code (for example: DE).

### os.getLanguage()
Returns the current two-letter ISO language code (for example: de).

### os.getLocale()
Returns the current locale in language_COUNTRY format (for example: de_DE).

### os.getPlatform()
Returns the current platform name: mac, linux, windows, or android.

### os.getTimeZone()
Returns the local time zone identifier (for example: Europe/Berlin).

### os.getUptime()
Returns seconds since SMS runtime initialization.

### os.isDesktop()
Returns true when running on a desktop platform, otherwise false.

### os.isMobile()
Returns true when running on a mobile platform (currently android), otherwise false.

### os.now()
Returns the current Unix epoch time in milliseconds.

## UI

### ui.getObject(name: String)
Returns a runtime object for any SML element with a matching id. Exposes callable methods dynamically from the underlying Godot object. Returns null when no matching id exists.

### ui.createDialog(path: String)
Creates a dialog from either a resource path (res:/...) or an inline SML definition string and returns the dialog instance.

### ui.createWindow(smlText: String)
Creates a floating window from inline SML text and returns a Window instance (or null on failure). The returned Window supports runtime actions onClose(callbackName) and close().

### Window Flag Constants
Enum-like global constants for Window flags in SMS. Use with setFlag/getFlag without magic numbers, e.g. mainWindow.setFlag(extendToTitle, true). Available constants: borderless=0, alwaysOnTop=1, transparent=2, noFocus=3, popup=5, extendToTitle=6, mousePassthrough=7, sharpCorners=8, excludeFromCapture=9, popupWmHint=10, minSize=11, maxSize=12, resizeDisabled=13, transient=14, modal=15, popupExclusive=16.

### <windowId>.getFlag(flag: int)
Gets a Window flag value by enum constant (recommended) or integer value. Example: var on = mainWindow.getFlag(extendToTitle).

### <windowId>.setFlag(flag: int, enabled: bool)
Sets a Window flag by enum constant (recommended) or integer value. Example: mainWindow.setFlag(extendToTitle, true).

