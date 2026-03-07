# ToDo
Here I will list things we need in general, like demos, tech previews-

- [ ] Default app, which will be loaded on startup id no URL is specified
- [ ] The designer app, which we can use to design and implement new apps, based on Forge
- [ ] Compiler that compiles SMS and SML to create native apps. Two phases convert to C# or create LLVM, depends on performance
- [ ] Packaging system (mac, win, linux) 
- [ ] Plugin Engine
- [ ] UserDefined Controls

## Native Core Track
- [ ] SMLCore in C++ (standalone lib, independent from ForgeRunner) -> `CWUP/tasks/smlcore_cpp_native_runtime.md`
- [ ] SMS in C++ (interpreter/runtime parity to current managed SMS) -> `CWUP/tasks/sms_cpp_native_runtime.md`
- [ ] SMS language spec upgrade (2b): implement new spec features in native runtime and keep managed/native behavior aligned -> `CWUP/tasks/sms_language_spec_2026_native.md`

## SMS Native Cutover Checklist
- [ ] Run apps in native-only mode (default, designer, poser)
- [ ] Verify no `Native SMS event dispatch failed...` errors in strict mode
- [ ] Verify no `Native SMS session is unavailable...` errors in strict mode
- [ ] Keep `SmsPerfLab --sms-conformance` green for managed/native parity
- [ ] Ensure all required `ui.*`/`os.*` bridge calls are supported by native runtime
- [ ] Remove managed event fallback path from `SmsUiRuntime.TryInvokeEvent`
- [ ] Remove managed `ScriptEngine.InvokeEvent` dependency after strict runs stay green

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

## Findings
- [ ] Investigate Godot shutdown leak report: `RID allocations of type TextureStorage::Texture were leaked at exit` (seen after app close; currently treated as engine-level diagnostic, not user-facing app error)
- [ ] Investigate Godot shutdown warning: `resources still in use at exit (run with --verbose for details)` and capture verbose details for concrete owner paths
- [ ] Verify cleanup/lifecycle for runtime viewports/textures (`SubViewport`, `ViewportTexture`, generated `ImageTexture`) on exit to reduce RID leak noise
- [ ] Fix verbose shutdown leak: `Cannot get path of node as it is not in a scene tree` with leaked UI nodes (`VBoxContainer`, `CodeEdit`, `HScrollBar`, `VScrollBar`, `MarginContainer`) during app exit
- [ ] Fix verbose shutdown leak: `Timer` instances leaked at exit (ensure timers created by runtime are canceled/freed on quit)
- [ ] Fix verbose shutdown leak: `SyntaxHighlighter` / `TextParagraph` / `FontFile` / `Image` reference leaks at exit (likely CodeEdit/theme/resource lifetime)
  

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

Logo in Terminal und https://github.com/magiblot/tvision


███████╗ ██████╗ ██████╗  ██████╗ ███████╗   ██╗  ██╗██████╗
██╔════╝██╔═══██╗██╔══██╗██╔════╝ ██╔════╝   ██║  ██║██╔══██╗
█████╗  ██║   ██║██████╔╝██║  ███╗█████╗     ███████║██║  ██║
██╔══╝  ██║   ██║██╔══██╗██║   ██║██╔══╝     ╚════██║██║  ██║
██║     ╚██████╔╝██║  ██║╚██████╔╝███████╗        ██║██████╔╝
╚═╝      ╚═════╝ ╚═╝  ╚═╝ ╚═════╝ ╚══════╝        ╚═╝╚═════╝

Built with love, coffee, and a stubborn focus on simplicity.