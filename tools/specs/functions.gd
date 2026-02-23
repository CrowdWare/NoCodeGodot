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