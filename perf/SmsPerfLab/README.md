# SmsPerfLab

Standalone performance lab for:

- `SML` parsing (managed baseline)
- `SMS` interpreting in C# (`SMSCore`)
- `SML` parsing in C++ (native spike)
- `SMS` interpreting in C++ (native spike)

This lab is intentionally independent from `ForgeRunner` runtime/UI integration.

## Structure

- `SmsPerfLab.csproj` - console benchmark harness
- `Program.cs` - benchmark runner
- `NativeSmsBridge.cs` - optional native bridge (`sms_native` shared library)
- `native/` - compatibility wrapper build (sources live in `SMSCore.Native/`)

## Build Native Spike

SML native parser:

```bash
cd SMLCore.Native
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build --config Release
```

Creates:

- macOS: `SMLCore.Native/build/libsmlcore_native.dylib`
- Linux: `SMLCore.Native/build/libsmlcore_native.so`
- Windows: `SMLCore.Native/build/Release/smlcore_native.dll` (generator-dependent)

SMS native interpreter:

```bash
cd SMSCore.Native
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build --config Release
```

This creates:

- macOS: `build/libsms_native.dylib`
- Linux: `build/libsms_native.so`
- Windows: `build/Release/sms_native.dll` (generator-dependent)

## Run Benchmark

From repo root:

```bash
dotnet run --project perf/SmsPerfLab/SmsPerfLab.csproj -- --iterations 200 --loop 20000
```

With native libraries:

```bash
SML_NATIVE_LIB_DIR="$(pwd)/SMLCore.Native/build" \
SMS_NATIVE_LIB_DIR="$(pwd)/SMSCore.Native/build" \
dotnet run --project perf/SmsPerfLab/SmsPerfLab.csproj -- --iterations 200 --loop 20000
```

## Run SML Conformance (Managed vs Native AST)

```bash
SML_NATIVE_LIB_DIR="$(pwd)/SMLCore.Native/build" \
dotnet run --project perf/SmsPerfLab/SmsPerfLab.csproj -- --conformance
```

Custom fixtures directory:

```bash
SML_NATIVE_LIB_DIR="$(pwd)/SMLCore.Native/build" \
dotnet run --project perf/SmsPerfLab/SmsPerfLab.csproj -- --conformance --fixtures "$(pwd)/perf/SmsPerfLab/fixtures/sml_conformance"
```

On Windows PowerShell:

```powershell
$env:SML_NATIVE_LIB_DIR = "$pwd/SMLCore.Native/build/Release"
$env:SMS_NATIVE_LIB_DIR = "$pwd/SMSCore.Native/build/Release"
dotnet run --project perf/SmsPerfLab/SmsPerfLab.csproj -- --iterations 200 --loop 20000
```

## Run SMS Conformance (Managed vs Native Execute)

```bash
SMS_NATIVE_LIB_DIR="$(pwd)/SMSCore.Native/build" \
dotnet run --project perf/SmsPerfLab/SmsPerfLab.csproj -- --sms-conformance
```

Custom fixtures directory:

```bash
SMS_NATIVE_LIB_DIR="$(pwd)/SMSCore.Native/build" \
dotnet run --project perf/SmsPerfLab/SmsPerfLab.csproj -- --sms-conformance --sms-fixtures "$(pwd)/perf/SmsPerfLab/fixtures/sms_conformance"
```

## Notes

- The C++ implementation is a spike subset parser/interpreter for benchmark scripts.
- SML native path now exposes AST JSON for conformance comparison, currently with a scalar-only subset.
- Native path is not feature-complete SMS/SML.
- Goal is to estimate headroom and validate migration direction before deeper integration.
