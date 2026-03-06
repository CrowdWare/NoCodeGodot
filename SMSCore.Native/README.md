# SMSCore.Native

Standalone native C++ runtime library for SMS experiments.

Current status:
- Provides C ABI entry points:
  - `sms_native_execute(...)`
  - `sms_native_sml_parse(...)` (bench helper path)
- Extracted from `perf/SmsPerfLab/native` so native SMS work can evolve independently from perf tooling.
- Intended as the foundation for full SMS native runtime integration in ForgeRunner.

Current runtime scope (phase 1):
- Minimal subset used by performance lab scripts.
- Not feature-parity with managed SMS interpreter yet.

## Build

```bash
cd SMSCore.Native
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build --config Release
```

## Run Native Spec Tests

```bash
cd SMSCore.Native
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release -DBUILD_TESTING=ON
cmake --build build --config Release
ctest --test-dir build --output-on-failure
```

Test suites include:
- `sms_native_spec_*` (SPEC_2026 coverage)
- `sms_parser_*` / `sms_events_*` (ported parser/runtime cases from `SMSCore.Native.Test`, limited to native runtime scope)

## Output

- macOS: `build/libsms_native.dylib`
- Linux: `build/libsms_native.so`
- Windows: `build/Release/sms_native.dll`
