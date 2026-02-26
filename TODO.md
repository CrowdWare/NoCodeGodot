# ToDo
Here I will list things we need in general, like demos, tech previews-

- [ ] Default app, which will be loaded on startup id no URL is specified
- [ ] The designer app, which we can use to design and implement new apps, based on Forge
- [ ] Compiler that compiles SMS and SML to create native apps. Two phases convert to C# or create LLVM, depends on performance
- [ ] Packaging system (mac, win, linux) 
- [ ] Plugin Engine

## Designer App
- [ ] Edit SML/SMS/MD
- [ ] Preview for MD, Zoom level
- [ ] Preview for SML (Windows, Dialogs), Zoom level
- [ ] Intellisense for SML
- [ ] Intellisense for SMS
- [ ] Visual Tree panel
- [ ] SML Element Inspector (like Godot/Unity inspector)
- [ ] Docking layout save/load (Save As Dialog, Load Dialog)
- [ ] Theme switch (Light/Dark)
- [ ] Undo/Redo file-based versioning
- [ ] Purge old file versions (after push)

## Default App
- [ ] Showcase demo app (showcase all UI elements)
- [ ] Performance demo
- [ ] 3D demo integration
- [ ] Markdown book demo
- [ ] Localization demo
- [ ] Plugin demo

## Ecosystem
- [ ] Template system (new app templates)
- [ ] App marketplace (later) 
- [ ] Plugin marketplace (later)
- [ ] Example repository
- [ ] Starter projects

## Vision-Level Stuff
- [ ] Server-mode runtime
- [ ] server.sml → Codegen → Kotlin Sources (KTOR)
- [ ] HumanityTree statt Database
  

### Database sample
```sml
Database {
    name: "ForgeServer"
    package: "com.crowdware.forge.server"

    Table {
        name: "User"
        Column { name: "id" type: "uuid" nullable: false }
        Column { name: "email" type: "string" nullable: false }
        Column { name: "createdAt" type: "instant" nullable: false }
        PrimaryKey { columns: "id" }
        Index { columns: "email" unique: true }
    }
}
```