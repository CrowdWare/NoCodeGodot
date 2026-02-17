extends SceneTree

var REPO_ROOT := "" # CHANGE
var SPEC_DIR := ""  # CHANGE
var OUT_PATH := ""  # CHANGE

func _initialize() -> void: # CHANGE
    REPO_ROOT = ProjectSettings.globalize_path("res://") + "/.." # CHANGE
    SPEC_DIR = REPO_ROOT + "/tools/specs" # CHANGE
    OUT_PATH = REPO_ROOT + "/docs/sms_functions.md" # CHANGE
    generate() # CHANGE
    quit() # CHANGE

func generate() -> void:
    var md := "# SMS Runtime Functions\n\n"
    md += "This document lists built-in SMS runtime helper functions available in NoCode.\n\n"

    var spec_path := SPEC_DIR + "/functions.gd" # CHANGE

    var spec_res := load(spec_path)
    if spec_res == null:
        var spec_uri := "file:///" + spec_path.substr(1)
        spec_res = load(spec_uri)

    if spec_res == null:
        push_error("Failed to load functions spec: " + spec_path)
        return

    var functions: Dictionary = spec_res.FUNCTIONS

    # Group functions by category
    var grouped := {}
    for name in functions.keys():
        var f: Dictionary = functions[name] # CHANGE
        var category := String(f.get("category", "Misc"))
        if not grouped.has(category):
            grouped[category] = []
        grouped[category].append(name)

    var categories := grouped.keys()
    categories.sort()

    for category in categories:
        md += "## %s\n\n" % category
        var names: Array = grouped[category] # CHANGE
        names.sort()

        for name in names:
            var f: Dictionary = functions[name] # CHANGE
            var signature := String(f.get("signature", name))
            var description := String(f.get("description", ""))

            md += "### %s\n" % signature
            md += "%s\n\n" % description

    var file := FileAccess.open(OUT_PATH, FileAccess.WRITE) # CHANGE
    if file:
        file.store_string(md)
        file.close()
        print("SMS functions documentation generated.")
    else:
        push_error("Failed to write SMS functions documentation.")

static func get_spec() -> Dictionary:
    return {
        "name": "sms_functions",
        "elements": []
    }