/*
 * Copyright (C) 2026 CrowdWare
 *
 * This file is part of ForgeRunner.
 *
 *  ForgeRunner is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  ForgeRunner is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with ForgeRunner.  If not, see <http://www.gnu.org/licenses/>.
 */

using Runtime.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Runtime.Sml;

public static class SmlParseRuntime
{
    private const int ErrorCapacity = 1024;
    private static bool _nativeProbeEnabled;

    public static void Configure(bool nativeProbeEnabled)
    {
        _nativeProbeEnabled = nativeProbeEnabled;
        RunnerLogger.Info("SML", $"Native parser probe: {(nativeProbeEnabled ? "enabled" : "disabled")}");
    }

    public static SmlDocument ParseDocument(string content, SmlParserSchema? schema = null, string context = "SML")
    {
        var nativeMs = -1L;
        long nativeNodes = -1;
        var nativeOk = false;

        if (_nativeProbeEnabled)
        {
            var swNative = Stopwatch.StartNew();
            nativeOk = NativeSmlProbe.TryParse(content, out nativeNodes);
            swNative.Stop();
            nativeMs = swNative.ElapsedMilliseconds;

            if (!nativeOk)
            {
                RunnerLogger.Warn("SML", $"Native probe failed in {context}: {NativeSmlProbe.LastError}");
            }
        }

        var swManaged = Stopwatch.StartNew();
        var parser = schema is null ? new SmlParser(content) : new SmlParser(content, schema);
        var doc = parser.ParseDocument();
        swManaged.Stop();

        if (_nativeProbeEnabled && nativeOk)
        {
            var managedNodes = CountNodes(doc);
            if (managedNodes != nativeNodes)
            {
                RunnerLogger.Warn("SML", $"Native/managed node mismatch in {context}: native={nativeNodes}, managed={managedNodes}");
            }
        }

        if (_nativeProbeEnabled)
        {
            RunnerLogger.Debug("Perf", $"[{context}] parse managed={swManaged.ElapsedMilliseconds}ms native={nativeMs}ms");
        }

        return doc;
    }

    private static long CountNodes(SmlDocument doc)
    {
        static long Count(SmlNode node)
        {
            long sum = 1;
            foreach (var child in node.Children)
            {
                sum += Count(child);
            }

            return sum;
        }

        long total = 0;
        foreach (var root in doc.Roots)
        {
            total += Count(root);
        }

        return total;
    }

    private static class NativeSmlProbe
    {
        private const string LibraryBaseName = "smlcore_native";
        private static bool _resolved;
        private static nint _libraryHandle;
        private static NativeParseFn? _parseFn;
        public static string LastError { get; private set; } = string.Empty;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NativeParseFn(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string source,
            out long nodeCount,
            StringBuilder error,
            int errorCapacity);

        public static bool TryParse(string source, out long nodeCount)
        {
            nodeCount = 0;
            if (!EnsureResolved() || _parseFn is null)
            {
                return false;
            }

            var error = new StringBuilder(ErrorCapacity);
            var rc = _parseFn(source, out nodeCount, error, ErrorCapacity);
            if (rc == 0)
            {
                return true;
            }

            LastError = error.Length > 0 ? error.ToString() : $"native rc={rc}";
            return false;
        }

        private static bool EnsureResolved()
        {
            if (_resolved)
            {
                return _parseFn is not null;
            }

            _resolved = true;

            foreach (var candidate in BuildCandidates())
            {
                if (!NativeLibrary.TryLoad(candidate, out _libraryHandle))
                {
                    continue;
                }

                if (!NativeLibrary.TryGetExport(_libraryHandle, "smlcore_native_parse", out var parsePtr))
                {
                    continue;
                }

                _parseFn = Marshal.GetDelegateForFunctionPointer<NativeParseFn>(parsePtr);
                if (_parseFn is not null)
                {
                    return true;
                }
            }

            LastError = "smlcore_native library not found";
            return false;
        }

        private static string[] BuildCandidates()
        {
            var ext = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? ".dll"
                : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    ? ".dylib"
                    : ".so";

            var envDir = Environment.GetEnvironmentVariable("SML_NATIVE_LIB_DIR");
            if (!string.IsNullOrWhiteSpace(envDir))
            {
                return
                [
                    Path.Combine(envDir, $"{LibraryBaseName}{ext}"),
                    Path.Combine(envDir, $"lib{LibraryBaseName}{ext}"),
                    $"{LibraryBaseName}{ext}",
                    $"lib{LibraryBaseName}{ext}"
                ];
            }

            return [$"{LibraryBaseName}{ext}", $"lib{LibraryBaseName}{ext}"];
        }
    }
}

