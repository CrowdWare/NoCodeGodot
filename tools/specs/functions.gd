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
        "signature": "ui.createWindow(path: String)",
        "description": "Creates a window from either a resource path (res:/...) or an inline SML definition string and returns the dialog instance."
    },

    "getObject": {
        "category": "UI",
        "signature": "ui.getObject(name: String)",
        "description": "Returns a reference to an object by id or name."
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