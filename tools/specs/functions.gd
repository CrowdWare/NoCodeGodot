extends RefCounted

# SMS Runtime Function Specification
# This file defines all built-in SMS functions in a structured format.

const FUNCTIONS := {
    "ui.createDialog": {
        "category": "UI",
        "signature": "ui.createDialog(path: String)",
        "description": "Creates a dialog from either a resource path (res:/...) or an inline SML definition string and returns the dialog instance."
    },

    "ui.createWindow": {
        "category": "UI",
        "signature": "ui.createWindow(smlText: String)",
        "description": "Creates a floating window from inline SML text and returns a Window instance (or null on failure). The returned Window supports runtime actions onClose(callbackName) and close()."
    },

    "getObject": {
        "category": "UI",
        "signature": "ui.getObject(name: String)",
        "description": "Returns a runtime object for any SML element with a matching id. Exposes callable methods dynamically from the underlying Godot object. Returns null when no matching id exists."
    },

    "window.flags": {
        "category": "UI",
        "signature": "Window Flag Constants",
        "description": "Enum-like global constants for Window flags in SMS. Use with setFlag/getFlag without magic numbers, e.g. mainWindow.setFlag(extendToTitle, true). Available constants: borderless=0, alwaysOnTop=1, transparent=2, noFocus=3, popup=5, extendToTitle=6, mousePassthrough=7, sharpCorners=8, excludeFromCapture=9, popupWmHint=10, minSize=11, maxSize=12, resizeDisabled=13, transient=14, modal=15, popupExclusive=16."
    },

    "window.setFlag": {
        "category": "UI",
        "signature": "<windowId>.setFlag(flag: int, enabled: bool)",
        "description": "Sets a Window flag by enum constant (recommended) or integer value. Example: mainWindow.setFlag(extendToTitle, true)."
    },

    "window.getFlag": {
        "category": "UI",
        "signature": "<windowId>.getFlag(flag: int)",
        "description": "Gets a Window flag value by enum constant (recommended) or integer value. Example: var on = mainWindow.getFlag(extendToTitle)."
    },

    "log.success": {
        "category": "Logging",
        "signature": "log.success(message: String)",
        "description": "Logs a success message."
    },

    "log.info": {
        "category": "Logging",
        "signature": "log.info(message: String)",
        "description": "Logs an informational message."
    },

    "log.warn": {
        "category": "Logging",
        "signature": "log.warn(message: String)",
        "description": "Logs a warning message."
    },

    "log.error": {
        "category": "Logging",
        "signature": "log.error(message: String)",
        "description": "Logs an error message."
    },

    "fs.list": {
        "category": "File System",
        "signature": "fs.list(path: String)",
        "description": "Lists files in a directory and returns an array of file names."
    },

    "fs.writeText": {
        "category": "File System",
        "signature": "fs.writeText(path: String, content: String)",
        "description": "Writes text content to a file."
    },

    "fs.readText": {
        "category": "File System",
        "signature": "fs.readText(path: String)",
        "description": "Reads text content from a file and returns it as a String."
    },

    "i18n.tr": {
        "category": "Internationalization",
        "signature": "i18n.tr(key: String, default: String?)",
        "description": "Returns the translation for the given key in the current locale. Falls back to the default value when provided, or to the key itself when no default is given and no translation is found."
    },

    "i18n.getLocale": {
        "category": "Internationalization",
        "signature": "i18n.getLocale()",
        "description": "Returns the active two-letter ISO language code (for example: en, de, es)."
    },

    "i18n.setLocale": {
        "category": "Internationalization",
        "signature": "i18n.setLocale(locale: String)",
        "description": "Switches the active locale and reloads the corresponding strings-<locale>.sml file. Subsequent i18n.tr() calls return translations for the new locale. Controls whose text was already rendered during UI build are not automatically updated; update them explicitly via script after calling setLocale. The call is a no-op when the requested locale is already active. A warning is logged on load failure and the previous locale is retained."
    },

    "os.getLocale": {
        "category": "OS",
        "signature": "os.getLocale()",
        "description": "Returns the current locale in language_COUNTRY format (for example: de_DE)."
    },

    "os.getLanguage": {
        "category": "OS",
        "signature": "os.getLanguage()",
        "description": "Returns the current two-letter ISO language code (for example: de)."
    },

    "os.getCountry": {
        "category": "OS",
        "signature": "os.getCountry()",
        "description": "Returns the current two-letter ISO country code (for example: DE)."
    },

    "os.getTimeZone": {
        "category": "OS",
        "signature": "os.getTimeZone()",
        "description": "Returns the local time zone identifier (for example: Europe/Berlin)."
    },

    "os.getPlatform": {
        "category": "OS",
        "signature": "os.getPlatform()",
        "description": "Returns the current platform name: mac, linux, windows, or android."
    },

    "os.getArch": {
        "category": "OS",
        "signature": "os.getArch()",
        "description": "Returns the current process architecture (for example: arm64 or x64)."
    },

    "os.isMobile": {
        "category": "OS",
        "signature": "os.isMobile()",
        "description": "Returns true when running on a mobile platform (currently android), otherwise false."
    },

    "os.isDesktop": {
        "category": "OS",
        "signature": "os.isDesktop()",
        "description": "Returns true when running on a desktop platform, otherwise false."
    },

    "os.now": {
        "category": "OS",
        "signature": "os.now()",
        "description": "Returns the current Unix epoch time in milliseconds."
    },

    "os.getUptime": {
        "category": "OS",
        "signature": "os.getUptime()",
        "description": "Returns seconds since SMS runtime initialization."
    }
}

# CHANGE: This file is NOT an SML element spec. It exists for SMS function docs.
# The SML element docs generator scans tools/specs/*.gd and expects get_spec().
# Return an empty spec so it can ignore this file without errors.
static func get_spec() -> Dictionary: # CHANGE
    return {
        "name": "sms_functions",
        "elements": []
    }