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

using Runtime.Sms;
using System.Globalization;

internal static class SmsConformance
{
    public static int Run(string fixturesDir, bool nativeOnly = false)
    {
        if (!Directory.Exists(fixturesDir))
        {
            Console.WriteLine($"Fixtures directory not found: {fixturesDir}");
            return 2;
        }

        var files = Directory
            .EnumerateFiles(fixturesDir, "*.sms", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (files.Length == 0)
        {
            Console.WriteLine($"No *.sms fixtures found in: {fixturesDir}");
            return 2;
        }

        var pass = 0;
        foreach (var file in files)
        {
            var label = Path.GetFileName(file);
            string source;
            try
            {
                source = File.ReadAllText(file);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FAIL] {label}");
                Console.WriteLine($"  Read error: {ex.Message}");
                continue;
            }

            if (!NativeSmsBridge.TryExecute(source, out var native))
            {
                Console.WriteLine($"[FAIL] {label}");
                Console.WriteLine($"  Native execute error: {NativeSmsBridge.LastError}");
                continue;
            }

            if (nativeOnly)
            {
                Console.WriteLine($"[OK]   {label}");
                pass++;
                continue;
            }

            long managed;
            try
            {
                var engine = new ScriptEngine();
                var result = engine.ExecuteAndGetDotNet(source);
                managed = ToInt64Result(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FAIL] {label}");
                Console.WriteLine($"  Managed execute error: {ex.Message}");
                continue;
            }

            if (managed != native)
            {
                Console.WriteLine($"[FAIL] {label}");
                Console.WriteLine($"  Result mismatch: managed={managed}, native={native}");
                continue;
            }

            Console.WriteLine($"[OK]   {label}");
            pass++;
        }

        Console.WriteLine();
        Console.WriteLine($"Conformance: {pass}/{files.Length} passing");
        return pass == files.Length ? 0 : 1;
    }

    private static long ToInt64Result(object? result)
    {
        return result switch
        {
            null => 0L,
            long value => value,
            int value => value,
            double value => Convert.ToInt64(value, CultureInfo.InvariantCulture),
            float value => Convert.ToInt64(value, CultureInfo.InvariantCulture),
            decimal value => Convert.ToInt64(value, CultureInfo.InvariantCulture),
            string value => long.Parse(value, CultureInfo.InvariantCulture),
            _ => Convert.ToInt64(result, CultureInfo.InvariantCulture)
        };
    }
}
