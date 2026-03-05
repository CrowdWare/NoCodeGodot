# SMLCore.Native

Standalone native C++ library for SML parsing experiments.

Current status:
- Provides a stable C ABI entry point: `smlcore_native_parse(...)`.
- Provides AST JSON export via `smlcore_native_parse_ast_json(...)`.
- Returns structural node count and AST for benchmark/conformance workloads.
- Intended as the first step toward full managed/native parser parity.

Current parser scope (phase 1):
- Elements with nested child elements
- Scalar properties (`string`, `bool`, `number`, `identifier`)
- Line comments (`// ...`)

Out of scope in this phase:
- tuples/vectors, resource refs, prop refs, component declarations

## Build

```bash
cd SMLCore.Native
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build --config Release
```

## Output

- macOS: `build/libsmlcore_native.dylib`
- Linux: `build/libsmlcore_native.so`
- Windows: `build/Release/smlcore_native.dll`
