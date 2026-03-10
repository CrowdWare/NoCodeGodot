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

internal static class NativeSmsBridge
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


    public static bool IsAvailable => IsSmsAvailable;
    public static bool IsSmsAvailable => EnsureResolved() && _executeFn is not null;
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
            return _libraryHandle != 0;
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

            if (_libraryHandle != 0)
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
