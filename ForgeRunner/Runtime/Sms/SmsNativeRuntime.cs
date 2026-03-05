using Runtime.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Runtime.Sms;

public static class SmsNativeRuntime
{
    private static bool _enabled;
    private static bool _probeCompleted;
    private static bool _available;
    private static bool _warnedMissing;
    private static string _lastError = string.Empty;

    public static bool Enabled => _enabled;
    public static bool Available => _available;
    public static string LastError => _lastError;

    public static void Configure(bool enabled)
    {
        _enabled = enabled;
        if (!_enabled)
        {
            RunnerLogger.Info("SMS", "Native runtime probe: disabled");
            return;
        }

        RunnerLogger.Info("SMS", "Native runtime probe: enabled");
    }

    public static bool EnsureProbed()
    {
        if (!_enabled)
        {
            return false;
        }

        if (_probeCompleted)
        {
            return _available;
        }

        _probeCompleted = true;
        if (!NativeSmsBridge.IsAvailable)
        {
            _available = false;
            _lastError = NativeSmsBridge.LastError;
            if (!_warnedMissing)
            {
                RunnerLogger.Warn("SMS", $"Native runtime unavailable: {_lastError}. Set SMS_NATIVE_LIB_DIR to enable.");
                _warnedMissing = true;
            }

            return false;
        }

        // Keep probe source within the tiny subset the C++ lab runtime already supports.
        const string probeSource = "var i = 0; for (var n = 0; n < 8; n = n + 1) { i = i + 1; } i;";
        if (!NativeSmsBridge.TryExecute(probeSource, out _))
        {
            _available = false;
            _lastError = NativeSmsBridge.LastError;
            RunnerLogger.Warn("SMS", $"Native runtime probe failed: {_lastError}");
            return false;
        }

        _available = true;
        _lastError = string.Empty;
        RunnerLogger.Info("SMS", "Native runtime probe OK (managed runtime still active).");
        return true;
    }

    private static class NativeSmsBridge
    {
        private const string LibraryBaseName = "sms_native";
        private const int ErrorCapacity = 2048;

        private static bool _resolved;
        private static nint _libraryHandle;
        private static NativeExecuteFn? _executeFn;
        private static string _lastError = string.Empty;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NativeExecuteFn(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string source,
            out long result,
            StringBuilder error,
            int errorCapacity);

        public static bool IsAvailable => EnsureResolved() && _executeFn is not null;
        public static string LastError => _lastError;

        public static bool TryExecute(string source, out long result)
        {
            result = 0;
            if (!EnsureResolved() || _executeFn is null)
            {
                return false;
            }

            var error = new StringBuilder(ErrorCapacity);
            var rc = _executeFn(source, out result, error, ErrorCapacity);
            if (rc != 0)
            {
                _lastError = error.Length > 0 ? error.ToString() : $"native rc={rc}";
                return false;
            }

            return true;
        }

        private static bool EnsureResolved()
        {
            if (_resolved)
            {
                return _executeFn is not null;
            }

            _resolved = true;

            foreach (var candidate in BuildCandidates())
            {
                if (!NativeLibrary.TryLoad(candidate, out _libraryHandle))
                {
                    continue;
                }

                if (NativeLibrary.TryGetExport(_libraryHandle, "sms_native_execute", out var executePtr))
                {
                    _executeFn = Marshal.GetDelegateForFunctionPointer<NativeExecuteFn>(executePtr);
                }

                if (_executeFn is not null)
                {
                    return true;
                }
            }

            _lastError = "sms_native library not found";
            return false;
        }

        private static IEnumerable<string> BuildCandidates()
        {
            var ext = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? ".dll"
                : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    ? ".dylib"
                    : ".so";

            var envDir = Environment.GetEnvironmentVariable("SMS_NATIVE_LIB_DIR");
            if (!string.IsNullOrWhiteSpace(envDir))
            {
                yield return Path.Combine(envDir, $"{LibraryBaseName}{ext}");
                yield return Path.Combine(envDir, $"lib{LibraryBaseName}{ext}");
            }

            yield return $"{LibraryBaseName}{ext}";
            yield return $"lib{LibraryBaseName}{ext}";
        }
    }
}

