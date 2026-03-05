# ForgePoser
![screenprint](/images/poser.png)


ForgePoser is the posing and scene authoring app built on top of Forge 4D.
This tool can also be used for short movie creation using grexboxing in the client and AI rendering in the cloud.

## Run

From the repository root:

```bash
./run.sh poser
```

## What You Can Do

- Build and edit character poses and animations
- Arrange scene content visually
- Save and load `.scene` files
- Export outputs through integrated tools/plugins

## Sources
Scene files are simple human read- and writeable SML files.
```qml
Scene {
    fps: 24
    totalFrames: 120
    
    Character {
        id: char1  name: "man"  src: "res:/assets/models/man.glb"
        pos: 2.007977, -0.01346169, 0.2265181
        rot: 5.7, -56.55, 0.0
        scale: 1.0, 0.9999998, 1.0

        Animation {
            Key { frame: 0
                Bone { name: "mixamorig1_LeftArm" x: 0.4910468 y: 0.0197618 z: -0.08403588 w: 0.8668452 }
                Bone { name: "mixamorig1_RightArm" x: 0.5174004 y: -0.02048415 z: 0.09174133 w: 0.850565 }
            }
        }
    }
}
```
## Available Plugins
Use these plugins also as reference.
Feel free to add feature requests.

- `promptPlugin` (`plugins/prompt`): Prompt panel docked in the left-bottom area. Supports image and short clip stylization workflows via Grok 4.
- `exportGlbPlugin` (`plugins/export_glb`): Provides GLB export functionality for ForgePoser.

---
Created with love, coffee, and Forge 4D.