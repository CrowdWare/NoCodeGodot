extends RefCounted

func get_spec() -> Dictionary:
    return {
        "name": "SplashScreen",
        "backing": "Panel",
        "notes": ["Startup screen shown before the main app loads. Shown immediately after entry files are downloaded. Remaining assets load in background with an optional ProgressBar child."],
        "properties": [
            {"sml":"id",            "type":"identifier",   "default":"—"},
            {"sml":"title",         "type":"string",       "default":"\"\""},
            {"sml":"size",          "type":"vec2i",        "default":"640, 480"},
            {"sml":"pos",           "type":"vec2i",        "default":"0, 0"},
            {"sml":"minSize",       "type":"vec2i",        "default":"0, 0"},
            {"sml":"extendToTitle", "type":"bool",         "default":"false"},
            {"sml":"duration",      "type":"int",          "default":"0",
             "notes":"Minimum display time in milliseconds before loading the next document"},
            {"sml":"loadOnReady",   "type":"string(url)",  "default":"\"\"",
             "notes":"SML document URL to load after duration has elapsed and all assets are ready"},
        ],
        "examples_sml": [
            "SplashScreen {",
            "    id: splash",
            "    size: 640, 480",
            "    duration: 3000",
            "    loadOnReady: \"res://docs/Default/main.sml\"",
            "",
            "    Label { text: \"Loading...\" }",
            "    ProgressBar {",
            "        id: downloadProgress",
            "        showPercentage: false",
            "        visible: false",
            "    }",
            "}",
        ],
    }
