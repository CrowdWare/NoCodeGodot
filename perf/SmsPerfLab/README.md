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
- `native/` - C++ SMS spike implementation

## Build Native Spike

```bash
cd perf/SmsPerfLab/native
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

With native library:

```bash
SMS_NATIVE_LIB_DIR="$(pwd)/perf/SmsPerfLab/native/build" \
dotnet run --project perf/SmsPerfLab/SmsPerfLab.csproj -- --iterations 200 --loop 20000
```

On Windows PowerShell:

```powershell
$env:SMS_NATIVE_LIB_DIR = "$pwd/perf/SmsPerfLab/native/build/Release"
dotnet run --project perf/SmsPerfLab/SmsPerfLab.csproj -- --iterations 200 --loop 20000
```

## Notes

- The C++ implementation is a spike subset parser/interpreter for benchmark scripts.
- SML native path currently benchmarks structural node scanning (hot-path approximation), not full SML semantics.
- Native path is not feature-complete SMS/SML.
- Goal is to estimate headroom and validate migration direction before deeper integration.
