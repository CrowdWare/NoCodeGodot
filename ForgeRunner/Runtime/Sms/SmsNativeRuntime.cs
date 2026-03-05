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
    public static bool SessionApiAvailable => NativeSmsBridge.HasSessionApi;
    public static string LastError => _lastError;
    public delegate bool UiGetPropertyBridge(string objectId, string property, out string valueJson, out string error);
    public delegate bool UiSetPropertyBridge(string objectId, string property, string valueJson, out string error);
    public delegate bool UiInvokeMethodBridge(string objectId, string method, string argsJson, out string resultJson, out string error);

    public static void Configure(bool enabled)
    {
        _enabled = enabled;
        if (!_enabled)
        {
            RunnerLogger.Warn("SMS", "Native runtime disabled via startup options, but native runtime is required.");
            return;
        }

        RunnerLogger.Info("SMS", "Native runtime probe: enabled (required mode)");
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
        RunnerLogger.Info("SMS", "Native runtime probe OK.");
        if (NativeSmsBridge.HasSessionApi)
        {
            if (!NativeSmsBridge.TryCreateSession(out var probeSession))
            {
                _available = false;
                _lastError = NativeSmsBridge.LastError;
                RunnerLogger.Warn("SMS", $"Native session API probe failed: {_lastError}");
                return false;
            }

            if (!NativeSmsBridge.TrySessionDispose(probeSession))
            {
                _available = false;
                _lastError = NativeSmsBridge.LastError;
                RunnerLogger.Warn("SMS", $"Native session API dispose probe failed: {_lastError}");
                return false;
            }

            RunnerLogger.Info("SMS", "Native session API available.");
        }
        else
        {
            RunnerLogger.Warn("SMS", "Native session API missing. Runner still uses managed ScriptEngine for events.");
        }
        return true;
    }

    public static bool ConfigureUiInterop(UiGetPropertyBridge? getProperty, UiSetPropertyBridge? setProperty, UiInvokeMethodBridge? invokeMethod)
    {
        if (!EnsureProbed())
        {
            return false;
        }

        if (!NativeSmsBridge.TrySetUiCallbacks(getProperty, setProperty, invokeMethod))
        {
            _lastError = NativeSmsBridge.LastError;
            return false;
        }

        return true;
    }

    public sealed class Session : IDisposable
    {
        private long _handle;
        private bool _disposed;

        private Session(long handle)
        {
            _handle = handle;
        }

        public static bool TryCreate(out Session? session)
        {
            session = null;
            if (!NativeSmsBridge.TryCreateSession(out var handle))
            {
                _lastError = NativeSmsBridge.LastError;
                return false;
            }

            session = new Session(handle);
            return true;
        }

        public bool TryLoad(string source)
        {
            if (_disposed)
            {
                _lastError = "native session already disposed";
                return false;
            }

            var ok = NativeSmsBridge.TrySessionLoad(_handle, source);
            if (!ok)
            {
                _lastError = NativeSmsBridge.LastError;
            }
            return ok;
        }

        public bool TryInvoke(string targetId, string eventName, string argsJson, out long result)
        {
            result = 0;
            if (_disposed)
            {
                _lastError = "native session already disposed";
                return false;
            }

            var ok = NativeSmsBridge.TrySessionInvoke(_handle, targetId, eventName, argsJson, out result);
            if (!ok)
            {
                _lastError = NativeSmsBridge.LastError;
            }
            return ok;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (!NativeSmsBridge.TrySessionDispose(_handle))
            {
                _lastError = NativeSmsBridge.LastError;
            }
            _handle = 0;
            _disposed = true;
        }
    }

    private static class NativeSmsBridge
    {
        private const string LibraryBaseName = "sms_native";
        private const int ErrorCapacity = 2048;

        private static bool _resolved;
        private static nint _libraryHandle;
        private static NativeExecuteFn? _executeFn;
        private static NativeSessionCreateFn? _sessionCreateFn;
        private static NativeSessionLoadFn? _sessionLoadFn;
        private static NativeSessionInvokeFn? _sessionInvokeFn;
        private static NativeSessionDisposeFn? _sessionDisposeFn;
        private static NativeSetUiCallbacksFn? _setUiCallbacksFn;
        private static UiGetPropertyBridge? _managedUiGetBridge;
        private static UiSetPropertyBridge? _managedUiSetBridge;
        private static UiInvokeMethodBridge? _managedUiInvokeBridge;
        private static NativeUiGetPropertyBridgeFn? _nativeUiGetBridge;
        private static NativeUiSetPropertyBridgeFn? _nativeUiSetBridge;
        private static NativeUiInvokeBridgeFn? _nativeUiInvokeBridge;
        private static string _lastError = string.Empty;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NativeExecuteFn(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string source,
            out long result,
            StringBuilder error,
            int errorCapacity);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NativeSessionCreateFn(
            out long session,
            StringBuilder error,
            int errorCapacity);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NativeSessionLoadFn(
            long session,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string source,
            StringBuilder error,
            int errorCapacity);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NativeSessionInvokeFn(
            long session,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string targetId,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string eventName,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string argsJson,
            out long result,
            StringBuilder error,
            int errorCapacity);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NativeSessionDisposeFn(
            long session,
            StringBuilder error,
            int errorCapacity);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NativeSetUiCallbacksFn(
            IntPtr getPropertyFn,
            IntPtr setPropertyFn,
            IntPtr invokeMethodFn,
            StringBuilder error,
            int errorCapacity);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NativeUiGetPropertyBridgeFn(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string objectId,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string property,
            StringBuilder outJson,
            int outJsonCapacity,
            StringBuilder error,
            int errorCapacity);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NativeUiSetPropertyBridgeFn(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string objectId,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string property,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string valueJson,
            StringBuilder error,
            int errorCapacity);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NativeUiInvokeBridgeFn(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string objectId,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string method,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string argsJson,
            StringBuilder outJson,
            int outJsonCapacity,
            StringBuilder error,
            int errorCapacity);

        public static bool IsAvailable => EnsureResolved() && _executeFn is not null;
        public static bool HasSessionApi =>
            EnsureResolved()
            && _sessionCreateFn is not null
            && _sessionLoadFn is not null
            && _sessionInvokeFn is not null
            && _sessionDisposeFn is not null;
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

        public static bool TryCreateSession(out long session)
        {
            session = 0;
            if (!EnsureResolved() || _sessionCreateFn is null)
            {
                _lastError = "native session API unavailable";
                return false;
            }

            var error = new StringBuilder(ErrorCapacity);
            var rc = _sessionCreateFn(out session, error, ErrorCapacity);
            if (rc != 0)
            {
                _lastError = error.Length > 0 ? error.ToString() : $"native rc={rc}";
                return false;
            }

            return true;
        }

        public static bool TrySessionLoad(long session, string source)
        {
            if (!EnsureResolved() || _sessionLoadFn is null)
            {
                _lastError = "native session API unavailable";
                return false;
            }

            var error = new StringBuilder(ErrorCapacity);
            var rc = _sessionLoadFn(session, source, error, ErrorCapacity);
            if (rc != 0)
            {
                _lastError = error.Length > 0 ? error.ToString() : $"native rc={rc}";
                return false;
            }

            return true;
        }

        public static bool TrySessionInvoke(long session, string targetId, string eventName, string argsJson, out long result)
        {
            result = 0;
            if (!EnsureResolved() || _sessionInvokeFn is null)
            {
                _lastError = "native session API unavailable";
                return false;
            }

            var error = new StringBuilder(ErrorCapacity);
            var rc = _sessionInvokeFn(session, targetId, eventName, argsJson, out result, error, ErrorCapacity);
            if (rc != 0)
            {
                _lastError = error.Length > 0 ? error.ToString() : $"native rc={rc}";
                return false;
            }

            return true;
        }

        public static bool TrySessionDispose(long session)
        {
            if (!EnsureResolved() || _sessionDisposeFn is null)
            {
                _lastError = "native session API unavailable";
                return false;
            }

            var error = new StringBuilder(ErrorCapacity);
            var rc = _sessionDisposeFn(session, error, ErrorCapacity);
            if (rc != 0)
            {
                _lastError = error.Length > 0 ? error.ToString() : $"native rc={rc}";
                return false;
            }

            return true;
        }

        public static bool TrySetUiCallbacks(UiGetPropertyBridge? getProperty, UiSetPropertyBridge? setProperty, UiInvokeMethodBridge? invokeMethod)
        {
            if (!EnsureResolved() || _setUiCallbacksFn is null)
            {
                _lastError = "native ui callback API unavailable";
                return false;
            }

            _managedUiGetBridge = getProperty;
            _managedUiSetBridge = setProperty;
            _managedUiInvokeBridge = invokeMethod;
            _nativeUiGetBridge = getProperty is null ? null : NativeUiGetPropertyThunk;
            _nativeUiSetBridge = setProperty is null ? null : NativeUiSetPropertyThunk;
            _nativeUiInvokeBridge = invokeMethod is null ? null : NativeUiInvokeThunk;

            var getPtr = _nativeUiGetBridge is null
                ? IntPtr.Zero
                : Marshal.GetFunctionPointerForDelegate(_nativeUiGetBridge);
            var setPtr = _nativeUiSetBridge is null
                ? IntPtr.Zero
                : Marshal.GetFunctionPointerForDelegate(_nativeUiSetBridge);
            var invokePtr = _nativeUiInvokeBridge is null
                ? IntPtr.Zero
                : Marshal.GetFunctionPointerForDelegate(_nativeUiInvokeBridge);

            var error = new StringBuilder(ErrorCapacity);
            var rc = _setUiCallbacksFn(getPtr, setPtr, invokePtr, error, ErrorCapacity);
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

                if (NativeLibrary.TryGetExport(_libraryHandle, "sms_native_session_create", out var createPtr))
                {
                    _sessionCreateFn = Marshal.GetDelegateForFunctionPointer<NativeSessionCreateFn>(createPtr);
                }

                if (NativeLibrary.TryGetExport(_libraryHandle, "sms_native_session_load", out var loadPtr))
                {
                    _sessionLoadFn = Marshal.GetDelegateForFunctionPointer<NativeSessionLoadFn>(loadPtr);
                }

                if (NativeLibrary.TryGetExport(_libraryHandle, "sms_native_session_invoke", out var invokePtr))
                {
                    _sessionInvokeFn = Marshal.GetDelegateForFunctionPointer<NativeSessionInvokeFn>(invokePtr);
                }

                if (NativeLibrary.TryGetExport(_libraryHandle, "sms_native_session_dispose", out var disposePtr))
                {
                    _sessionDisposeFn = Marshal.GetDelegateForFunctionPointer<NativeSessionDisposeFn>(disposePtr);
                }

                if (NativeLibrary.TryGetExport(_libraryHandle, "sms_native_set_ui_callbacks", out var setUiCallbacksPtr))
                {
                    _setUiCallbacksFn = Marshal.GetDelegateForFunctionPointer<NativeSetUiCallbacksFn>(setUiCallbacksPtr);
                }

                if (_executeFn is not null)
                {
                    return true;
                }
            }

            _lastError = "sms_native library not found";
            return false;
        }

        private static int NativeUiGetPropertyThunk(
            string objectId,
            string property,
            StringBuilder outJson,
            int outJsonCapacity,
            StringBuilder error,
            int errorCapacity)
        {
            if (_managedUiGetBridge is null)
            {
                error.Append("ui get bridge not set");
                return 2;
            }

            try
            {
                if (!_managedUiGetBridge(objectId, property, out var valueJson, out var err))
                {
                    if (!string.IsNullOrWhiteSpace(err))
                    {
                        error.Append(err);
                    }
                    return 1;
                }

                outJson.Append(valueJson ?? "null");
                return 0;
            }
            catch (Exception ex)
            {
                error.Append(ex.Message);
                return 1;
            }
        }

        private static int NativeUiSetPropertyThunk(
            string objectId,
            string property,
            string valueJson,
            StringBuilder error,
            int errorCapacity)
        {
            if (_managedUiSetBridge is null)
            {
                error.Append("ui set bridge not set");
                return 2;
            }

            try
            {
                if (!_managedUiSetBridge(objectId, property, valueJson, out var err))
                {
                    if (!string.IsNullOrWhiteSpace(err))
                    {
                        error.Append(err);
                    }
                    return 1;
                }

                return 0;
            }
            catch (Exception ex)
            {
                error.Append(ex.Message);
                return 1;
            }
        }

        private static int NativeUiInvokeThunk(
            string objectId,
            string method,
            string argsJson,
            StringBuilder outJson,
            int outJsonCapacity,
            StringBuilder error,
            int errorCapacity)
        {
            if (_managedUiInvokeBridge is null)
            {
                error.Append("ui invoke bridge not set");
                return 2;
            }

            try
            {
                if (!_managedUiInvokeBridge(objectId, method, argsJson, out var resultJson, out var err))
                {
                    if (!string.IsNullOrWhiteSpace(err))
                    {
                        error.Append(err);
                    }
                    return 1;
                }

                outJson.Append(resultJson ?? "null");
                return 0;
            }
            catch (Exception ex)
            {
                error.Append(ex.Message);
                return 1;
            }
        }

        private static IEnumerable<string> BuildCandidates()
        {
            var ext = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? ".dll"
                : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    ? ".dylib"
                    : ".so";
            var fileA = $"{LibraryBaseName}{ext}";
            var fileB = $"lib{LibraryBaseName}{ext}";

            var envDir = Environment.GetEnvironmentVariable("SMS_NATIVE_LIB_DIR");
            if (!string.IsNullOrWhiteSpace(envDir))
            {
                yield return Path.Combine(envDir, fileA);
                yield return Path.Combine(envDir, fileB);
            }

            foreach (var dir in EnumerateFallbackDirs())
            {
                yield return Path.Combine(dir, fileA);
                yield return Path.Combine(dir, fileB);
            }

            yield return fileA;
            yield return fileB;
        }

        private static IEnumerable<string> EnumerateFallbackDirs()
        {
            // Dev-friendly fallbacks so local runs work without manually exporting SMS_NATIVE_LIB_DIR.
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            static IEnumerable<string> ExpandParents(string? startDir)
            {
                if (string.IsNullOrWhiteSpace(startDir))
                {
                    yield break;
                }

                var current = Path.GetFullPath(startDir);
                for (var i = 0; i < 6 && !string.IsNullOrWhiteSpace(current); i++)
                {
                    yield return current;
                    var parent = Directory.GetParent(current);
                    if (parent is null)
                    {
                        yield break;
                    }
                    current = parent.FullName;
                }
            }

            foreach (var root in ExpandParents(AppContext.BaseDirectory))
            {
                var candidate = Path.Combine(root, "SMSCore.Native", "build");
                if (seen.Add(candidate))
                {
                    yield return candidate;
                }
            }

            foreach (var root in ExpandParents(Directory.GetCurrentDirectory()))
            {
                var candidate = Path.Combine(root, "SMSCore.Native", "build");
                if (seen.Add(candidate))
                {
                    yield return candidate;
                }
            }
        }
    }
}
