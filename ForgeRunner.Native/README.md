# ForgeRunner.Native

Native Godot host scaffold for the ForgeRunner C++ migration.

Current scope:
- Provides a minimal GDExtension entrypoint (`forge_runner_native_library_init`).
- Registers `ForgeRunnerNativeMain` as a native scene-level class.
- Establishes an isolated build path used by `./run.sh build-native-host`.
- Provides a native executable entrypoint (`forge-runner-native`) in C++.

## Prerequisites

- CMake 3.20+
- C++17 compiler
- `godot-cpp` checkout with generated bindings and built static library

Set:

```bash
export GODOT_CPP_DIR=/absolute/path/to/godot-cpp
```

## Build

```bash
cmake -S ForgeRunner.Native -B ForgeRunner.Native/build -DCMAKE_BUILD_TYPE=Release -DGODOT_CPP_DIR="$GODOT_CPP_DIR"
cmake --build ForgeRunner.Native/build --config Release
```

`./run.sh build-native-host` copies produced artifacts into `ForgeRunner.Native/dist/`:

- macOS: `libforge_runner_native.dylib`
- Linux: `libforge_runner_native.so`
- Windows: `forge_runner_native.dll`
- Executable: `forge-runner-native` (`forge-runner-native.exe` on Windows)
