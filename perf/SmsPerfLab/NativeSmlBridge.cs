/*
#############################################################################
# Copyright (C) 2026 CrowdWare
#
# This file is part of Forge.
#
# SPDX-License-Identifier: GPL-3.0-or-later OR LicenseRef-CrowdWare-Commercial
#
# Forge is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
#
# Forge is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with Forge. If not, see <https://www.gnu.org/licenses/>.
#
# Commercial licensing is available from CrowdWare for proprietary use.
#############################################################################
*/

using System.Runtime.InteropServices;
using System.Text;

internal static class NativeSmlBridge
{
    private const string LibraryBaseName = "smlcore_native";
    private const int ErrorCapacity = 2048;

    private static bool _resolved;
    private static nint _libraryHandle;
    private static NativeParseSmlFn? _parseSmlFn;
    private static NativeParseAstJsonFn? _parseAstJsonFn;
    private static string _lastError = string.Empty;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NativeParseSmlFn(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string source,
        out long nodeCount,
        StringBuilder error,
        int errorCapacity);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NativeParseAstJsonFn(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string source,
        StringBuilder outputJson,
        int outputJsonCapacity,
        out long outputJsonLength,
        StringBuilder error,
        int errorCapacity);

    public static bool IsAvailable => EnsureResolved() && _parseSmlFn is not null;
    public static bool IsAstAvailable => EnsureResolved() && _parseAstJsonFn is not null;
    public static string LastError => _lastError;

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

    public static bool TryParseAstJson(string source, out string astJson)
    {
        astJson = string.Empty;
        if (!EnsureResolved() || _parseAstJsonFn is null)
        {
            return false;
        }

        const int initialCapacity = 64 * 1024;
        var output = new StringBuilder(initialCapacity);
        var error = new StringBuilder(ErrorCapacity);
        var rc = _parseAstJsonFn(source, output, output.Capacity, out var expectedLength, error, ErrorCapacity);
        if (rc == 0)
        {
            astJson = output.ToString();
            return true;
        }

        if (rc == 2 && expectedLength > output.Capacity)
        {
            var retryCapacity = checked((int)Math.Min(expectedLength + 1, 4 * 1024 * 1024));
            output = new StringBuilder(retryCapacity);
            error.Clear();
            rc = _parseAstJsonFn(source, output, output.Capacity, out expectedLength, error, ErrorCapacity);
            if (rc == 0)
            {
                astJson = output.ToString();
                return true;
            }
        }

        _lastError = error.Length > 0 ? error.ToString() : $"native rc={rc}";
        return false;
    }

    private static bool EnsureResolved()
    {
        if (_resolved)
        {
            return _parseSmlFn is not null;
        }

        _resolved = true;

        foreach (var candidate in BuildCandidates())
        {
            if (!NativeLibrary.TryLoad(candidate, out _libraryHandle))
            {
                continue;
            }

            if (NativeLibrary.TryGetExport(_libraryHandle, "smlcore_native_parse", out var parseSmlPtr))
            {
                _parseSmlFn = Marshal.GetDelegateForFunctionPointer<NativeParseSmlFn>(parseSmlPtr);
            }

            if (NativeLibrary.TryGetExport(_libraryHandle, "smlcore_native_parse_ast_json", out var parseAstJsonPtr))
            {
                _parseAstJsonFn = Marshal.GetDelegateForFunctionPointer<NativeParseAstJsonFn>(parseAstJsonPtr);
            }

            if (_parseSmlFn is not null || _parseAstJsonFn is not null)
            {
                return true;
            }
        }

        _lastError = "smlcore_native library not found";
        return false;
    }

    private static IEnumerable<string> BuildCandidates()
    {
        var ext = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? ".dll"
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? ".dylib"
                : ".so";

        var envDir = Environment.GetEnvironmentVariable("SML_NATIVE_LIB_DIR");
        if (!string.IsNullOrWhiteSpace(envDir))
        {
            yield return Path.Combine(envDir, $"{LibraryBaseName}{ext}");
            yield return Path.Combine(envDir, $"lib{LibraryBaseName}{ext}");
        }

        yield return $"{LibraryBaseName}{ext}";
        yield return $"lib{LibraryBaseName}{ext}";
    }
}
