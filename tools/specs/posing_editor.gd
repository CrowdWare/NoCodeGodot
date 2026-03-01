extends RefCounted

func get_spec() -> Dictionary:
    return {
        "name": "PosingEditor",
        "backing": "SubViewportContainer",
        "properties": [
            {"sml":"id",              "type":"identifier", "default":"—"},
            {"sml":"src",             "type":"string(url)", "default":"\"\""},
            {"sml":"showBoneTree",    "type":"bool",        "default":"false"},
            {"sml":"normalizeNames",  "type":"bool",        "default":"true"},
        ],
        "children": [
            {
                "name": "JointConstraint",
                "properties": [
                    {"sml":"bone", "type":"string",  "default":"\"\""},
                    {"sml":"minX", "type":"float",   "default":"-180"},
                    {"sml":"maxX", "type":"float",   "default":"180"},
                    {"sml":"minY", "type":"float",   "default":"-180"},
                    {"sml":"maxY", "type":"float",   "default":"180"},
                    {"sml":"minZ", "type":"float",   "default":"-180"},
                    {"sml":"maxZ", "type":"float",   "default":"180"},
                ],
            },
        ],
        "actions": [
            {"sms":"resetPose",           "params":[], "returns":"void"},
            {"sms":"setJointSpheresVisible", "params":[{"name":"visible","type":"bool"}], "returns":"void"},
            {"sms":"setBoneTree",         "params":[{"name":"treeId","type":"string"}], "returns":"void"},
            {"sms":"setModelSource",      "params":[{"name":"src","type":"string"}], "returns":"void"},
            {"sms":"loadProject",         "params":[{"name":"path","type":"string"}], "returns":"void"},
            {"sms":"saveProject",         "params":[{"name":"path","type":"string"}], "returns":"void"},
            {"sms":"addSceneProp",        "params":[{"name":"path","type":"string"},{"name":"posX","type":"float"},{"name":"posY","type":"float"},{"name":"posZ","type":"float"}], "returns":"int"},
            {"sms":"removeSceneProp",     "params":[{"name":"index","type":"int"}], "returns":"void"},
            {"sms":"getScenePropCount",   "params":[], "returns":"int"},
            {"sms":"getScenePropPath",    "params":[{"name":"index","type":"int"}], "returns":"string"},
            {"sms":"getScenePropName",    "params":[{"name":"index","type":"int"}], "returns":"string"},
            {"sms":"getScenePropPos",     "params":[{"name":"index","type":"int"}], "returns":"string", "note":"Returns 'x,y,z' string"},
            {"sms":"setScenePropPos",     "params":[{"name":"index","type":"int"},{"name":"x","type":"float"},{"name":"y","type":"float"},{"name":"z","type":"float"}], "returns":"void"},
            {"sms":"getScenePropRot",     "params":[{"name":"index","type":"int"}], "returns":"string", "note":"Returns 'x,y,z' Euler degrees"},
            {"sms":"setScenePropRot",     "params":[{"name":"index","type":"int"},{"name":"x","type":"float"},{"name":"y","type":"float"},{"name":"z","type":"float"}], "returns":"void"},
            {"sms":"setMode",             "params":[{"name":"mode","type":"string"}], "returns":"void", "note":"Values: 'pose' | 'arrange'. Clears all gizmos and selections."},
            {"sms":"setEditMode",         "params":[{"name":"mode","type":"string"}], "returns":"void", "note":"Values: 'move' | 'scale' | 'rotate'. Only effective in Arrange mode."},
        ],
        "events": [
            {"sms":"boneSelected",    "params":[{"name":"boneName","type":"string"}]},
            {"sms":"poseChanged",     "params":[{"name":"boneName","type":"string"}]},
            {"sms":"poseReset",       "params":[]},
            {"sms":"scenePropAdded",  "params":[{"name":"index","type":"int"},{"name":"path","type":"string"}]},
            {"sms":"scenePropRemoved","params":[{"name":"index","type":"int"}]},
            {"sms":"objectSelected",  "params":[{"name":"propIdx","type":"int"}], "note":"-1 = deselect"},
            {"sms":"objectMoved",     "params":[{"name":"propIdx","type":"int"},{"name":"pos","type":"string"}], "note":"pos = 'x,y,z'"},
        ],
        "dynamic_properties": [
            {"sms":"poseData", "type":"object", "note":"Dictionary<boneName, Quaternion> — use with setKeyframe/loadPose"},
            {"sms":"currentFrame", "type":"int"},
        ],
    }
