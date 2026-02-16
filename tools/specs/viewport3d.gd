extends RefCounted

func get_spec() -> Dictionary:
    return {
        "name": "Viewport3D",
        "backing": "SubViewportContainer",
        "properties": [
            {"sml":"id", "type":"identifier", "default":"—"},
            {"sml":"model", "type":"string(url)", "default":"\"\""},
            {"sml":"modelSource", "type":"string(url)", "default":"\"\""},
            {"sml":"animation", "type":"string(url)", "default":"\"\""},
            {"sml":"animationSource", "type":"string(url)", "default":"\"\""},
            {"sml":"playAnimation", "type":"int", "default":"0"},
            {"sml":"playFirstAnimation", "type":"bool", "default":"false"},
            {"sml":"autoplayAnimation", "type":"bool", "default":"false"},
            {"sml":"defaultAnimation", "type":"string", "default":"\"\""},
            {"sml":"playLoop", "type":"bool", "default":"false"},
            {"sml":"cameraDistance", "type":"int", "default":"0"},
            {"sml":"lightEnergy", "type":"int", "default":"0"},
        ],
        "actions": [
            {"sms":"playAnimation", "params":[{"name":"index","type":"int"}], "returns":"void"},
            {"sms":"stopAnimation", "params":[], "returns":"void"},
            {"sms":"rewind", "params":[], "returns":"void"},
            {"sms":"setFrame", "params":[{"name":"frame","type":"int"}], "returns":"void"},
            {"sms":"zoomIn", "params":[], "returns":"void"},
            {"sms":"zoomOut", "params":[], "returns":"void"},
        ],
    }