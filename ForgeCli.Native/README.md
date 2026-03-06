# ForgeCli.Native

Minimal C++ rewrite of ForgeCli.

Current commands:
- `new`
- `validate` (via `smlcore_native` + `sms_native`)

Build:

```bash
cmake -S ForgeCli.Native -B ForgeCli.Native/build -DCMAKE_BUILD_TYPE=Release
cmake --build ForgeCli.Native/build --config Release
```

Run:

```bash
SML_NATIVE_LIB_DIR="$(pwd)/SMLCore.Native/build" \
SMS_NATIVE_LIB_DIR="$(pwd)/SMSCore.Native/build" \
./ForgeCli.Native/build/forgecli-native validate --project ./MyApp
```

