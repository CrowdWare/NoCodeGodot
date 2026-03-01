extends RefCounted

func get_spec() -> Dictionary:
    return {
        "name": "Timeline",
        "backing": "Control",
        "properties": [
            {"sml":"id",           "type":"identifier", "default":"—"},
            {"sml":"fps",          "type":"int",        "default":"24"},
            {"sml":"totalFrames",  "type":"int",        "default":"120"},
        ],
        "actions": [
            {"sms":"play",              "params":[], "returns":"void"},
            {"sms":"stop",              "params":[], "returns":"void"},
            {"sms":"setKeyframe",       "params":[{"name":"frame","type":"int"},{"name":"poseData","type":"object"}], "returns":"void"},
            {"sms":"removeKeyframe",    "params":[{"name":"frame","type":"int"}], "returns":"void"},
            {"sms":"getPoseAt",         "params":[{"name":"frame","type":"int"}], "returns":"object"},
            {"sms":"getKeyframeCount",  "params":[], "returns":"int"},
            {"sms":"getKeyframeFrameAt","params":[{"name":"index","type":"int"}], "returns":"int"},
            {"sms":"getKeyframeBoneCount","params":[{"name":"frame","type":"int"}], "returns":"int"},
            {"sms":"getKeyframeBoneName","params":[{"name":"frame","type":"int"},{"name":"boneIndex","type":"int"}], "returns":"string"},
            {"sms":"clearAllKeyframes", "params":[], "returns":"void"},
        ],
        "events": [
            {"sms":"frameChanged",     "params":[{"name":"frame","type":"int"}]},
            {"sms":"keyframeAdded",    "params":[{"name":"frame","type":"int"},{"name":"boneName","type":"string"}]},
            {"sms":"keyframeRemoved",  "params":[{"name":"frame","type":"int"}]},
            {"sms":"playbackStarted",  "params":[]},
            {"sms":"playbackStopped",  "params":[]},
        ],
        "dynamic_properties": [
            {"sms":"currentFrame", "type":"int"},
            {"sms":"fps",          "type":"int"},
            {"sms":"totalFrames",  "type":"int"},
        ],
    }
