using System.Runtime.InteropServices;
using System.Text;

internal static class NativeSmsBridge
{
    private const string LibraryBaseName = "sms_native";
    private const int ErrorCapacity = 2048;

    private static bool _resolved;
    private static nint _libraryHandle;
    private static NativeExecuteFn? _executeFn;
    private static NativeParseSmlFn? _parseSmlFn;
    private static string _lastError = string.Empty;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NativeExecuteFn(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string source,
        out long result,
        StringBuilder error,
        int errorCapacity);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NativeParseSmlFn(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string source,
        out long nodeCount,
        StringBuilder error,
        int errorCapacity);

    public static bool IsAvailable => IsSmsAvailable;
    public static bool IsSmsAvailable => EnsureResolved() && _executeFn is not null;
    public static bool IsSmlAvailable => EnsureResolved() && _parseSmlFn is not null;
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

    public static bool TryParseSml(string source, out long nodeCount)
    {
        nodeCount = 0;
        if (!EnsureResolved() || _parseSmlFn is null)
        {
            return false;
        }

        var error = new StringBuilder(ErrorCapacity);
        var rc = _parseSmlFn(source, out nodeCount, error, ErrorCapacity);
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
            return _executeFn is not null || _parseSmlFn is not null;
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

            if (NativeLibrary.TryGetExport(_libraryHandle, "sms_native_sml_parse", out var parseSmlPtr))
            {
                _parseSmlFn = Marshal.GetDelegateForFunctionPointer<NativeParseSmlFn>(parseSmlPtr);
            }

            if (_executeFn is not null || _parseSmlFn is not null)
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
