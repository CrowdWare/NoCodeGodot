# export_glb Plugin

This plugin demonstrates an export plugin with SMS + C# assembly.

## Build

```bash
cd ForgePoser/plugins/export_glb/ExportGlbPlugin
dotnet build
```

The build emits `ExportGlbPlugin.dll` into `ForgePoser/plugins/export_glb/`.

## Runtime usage

`panel.sms` calls:
- `ExportGlbPlugin.EntryPoints.ResolveOutputPath(...)`
- `ExportGlbPlugin.EntryPoints.GetIncludeAnimation(...)`
- `ExportGlbPlugin.EntryPoints.GetIncludeProps(...)`
- `ExportGlbPlugin.EntryPoints.GetAnimationOnlyCharacter(...)`
