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

using Runtime.Sml;
using Runtime.Sms;
using System.Diagnostics;

internal static class Program
{
    private static int Main(string[] args)
    {
        var iterations = 200;
        var loopCount = 20_000;
        var runSmlConformance = false;
        var runSmsConformance = false;
        var runSmsConformanceNativeOnly = false;
        var smlConformanceDir = Path.Combine(Directory.GetCurrentDirectory(), "perf", "SmsPerfLab", "fixtures", "sml_conformance");
        var smsConformanceDir = Path.Combine(Directory.GetCurrentDirectory(), "perf", "SmsPerfLab", "fixtures", "sms_conformance");

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--iterations" && i + 1 < args.Length && int.TryParse(args[i + 1], out var parsedIterations))
            {
                iterations = Math.Max(1, parsedIterations);
                i++;
                continue;
            }

            if (args[i] == "--loop" && i + 1 < args.Length && int.TryParse(args[i + 1], out var parsedLoop))
            {
                loopCount = Math.Max(1, parsedLoop);
                i++;
                continue;
            }

            if (args[i] == "--conformance")
            {
                runSmlConformance = true;
                continue;
            }

            if (args[i] == "--sms-conformance")
            {
                runSmsConformance = true;
                continue;
            }

            if (args[i] == "--sms-conformance-native-only")
            {
                runSmsConformance = true;
                runSmsConformanceNativeOnly = true;
                continue;
            }

            if (args[i] == "--fixtures" && i + 1 < args.Length)
            {
                smlConformanceDir = args[i + 1];
                i++;
                continue;
            }

            if (args[i] == "--sms-fixtures" && i + 1 < args.Length)
            {
                smsConformanceDir = args[i + 1];
                i++;
            }
        }

        if (runSmlConformance)
        {
            Console.WriteLine("SmsPerfLab - SML Conformance");
            Console.WriteLine($"  fixtures: {smlConformanceDir}");
            Console.WriteLine();
            return SmlConformance.Run(smlConformanceDir);
        }

        if (runSmsConformance)
        {
            Console.WriteLine("SmsPerfLab - SMS Conformance");
            Console.WriteLine($"  fixtures: {smsConformanceDir}");
            Console.WriteLine($"  nativeOnly: {runSmsConformanceNativeOnly}");
            Console.WriteLine();
            return SmsConformance.Run(smsConformanceDir, runSmsConformanceNativeOnly);
        }

        Console.WriteLine("SmsPerfLab");
        Console.WriteLine($"  iterations: {iterations}");
        Console.WriteLine($"  loopCount:  {loopCount}");
        Console.WriteLine();

        var smlSource = BuildSmlDocument(2_000);
        var smsSource = BuildSmsScript(loopCount);
        var expected = (long)loopCount * (loopCount - 1) / 2;

        // Warm-up
        _ = new SmlParser(smlSource).ParseDocument();
        var warmEngine = new ScriptEngine();
        _ = warmEngine.ExecuteAndGetDotNet(smsSource);
        NativeSmlBridge.TryParseSml(smlSource, out _);
        NativeSmsBridge.TryExecute(smsSource, out _);

        var smlResult = BenchmarkSmlParsing(smlSource, iterations);
        var hasSmlNative = NativeSmlBridge.IsAvailable;
        var smlNativeResult = hasSmlNative ? BenchmarkSmlNative(smlSource, iterations) : (ElapsedMs: -1.0, Nodes: 0L);
        var smsManagedResult = BenchmarkSmsManaged(smsSource, iterations);
        var hasSmsNative = NativeSmsBridge.IsSmsAvailable;
        var smsNativeResult = hasSmsNative ? BenchmarkSmsNative(smsSource, iterations) : (ElapsedMs: -1.0, Result: 0L);

        Validate("SML managed", smlResult.Nodes, smlResult.Nodes);
        if (hasSmlNative)
        {
            Validate("SML native", smlNativeResult.Nodes, smlResult.Nodes);
        }
        Validate("SMS managed", smsManagedResult.Result, expected);
        if (hasSmsNative)
        {
            Validate("SMS native", smsNativeResult.Result, expected);
        }

        Console.WriteLine("Results");
        PrintRow("SML parse (managed)", smlResult.ElapsedMs, iterations);
        if (hasSmlNative)
        {
            PrintRow("SML parse (native C++)", smlNativeResult.ElapsedMs, iterations);
            var smlSpeedup = smlResult.ElapsedMs / Math.Max(0.0001, smlNativeResult.ElapsedMs);
            Console.WriteLine($"  SML speedup (managed/native): {smlSpeedup:F2}x");
        }
        else
        {
            var reason = string.IsNullOrWhiteSpace(NativeSmlBridge.LastError) ? "symbol not found" : NativeSmlBridge.LastError;
            Console.WriteLine($"  SML parse (native C++): skipped ({reason})");
        }

        PrintRow("SMS interpret (managed)", smsManagedResult.ElapsedMs, iterations);
        if (hasSmsNative)
        {
            PrintRow("SMS interpret (native C++)", smsNativeResult.ElapsedMs, iterations);
            var speedup = smsManagedResult.ElapsedMs / Math.Max(0.0001, smsNativeResult.ElapsedMs);
            Console.WriteLine($"  SMS speedup (managed/native): {speedup:F2}x");
        }
        else
        {
            var reason = string.IsNullOrWhiteSpace(NativeSmsBridge.LastError) ? "library not found" : NativeSmsBridge.LastError;
            Console.WriteLine($"  SMS interpret (native C++): skipped ({reason})");
            Console.WriteLine("  set SMS_NATIVE_LIB_DIR to enable native benchmark");
        }

        return 0;
    }

    private static (double ElapsedMs, long Nodes) BenchmarkSmlParsing(string source, int iterations)
    {
        var sw = Stopwatch.StartNew();
        long nodes = 0;
        for (var i = 0; i < iterations; i++)
        {
            var doc = new SmlParser(source).ParseDocument();
            nodes = CountNodes(doc);
        }

        sw.Stop();
        return (sw.Elapsed.TotalMilliseconds, nodes);
    }

    private static (double ElapsedMs, long Nodes) BenchmarkSmlNative(string source, int iterations)
    {
        var sw = Stopwatch.StartNew();
        long nodes = 0;
        for (var i = 0; i < iterations; i++)
        {
            if (!NativeSmlBridge.TryParseSml(source, out nodes))
            {
                throw new InvalidOperationException($"Native SML error: {NativeSmlBridge.LastError}");
            }
        }

        sw.Stop();
        return (sw.Elapsed.TotalMilliseconds, nodes);
    }

    private static (double ElapsedMs, long Result) BenchmarkSmsManaged(string source, int iterations)
    {
        var sw = Stopwatch.StartNew();
        long result = 0;
        for (var i = 0; i < iterations; i++)
        {
            var engine = new ScriptEngine();
            var output = engine.ExecuteAndGetDotNet(source);
            result = Convert.ToInt64(output);
        }

        sw.Stop();
        return (sw.Elapsed.TotalMilliseconds, result);
    }

    private static (double ElapsedMs, long Result) BenchmarkSmsNative(string source, int iterations)
    {
        var sw = Stopwatch.StartNew();
        long result = 0;
        for (var i = 0; i < iterations; i++)
        {
            if (!NativeSmsBridge.TryExecute(source, out result))
            {
                throw new InvalidOperationException($"Native SMS error: {NativeSmsBridge.LastError}");
            }
        }

        sw.Stop();
        return (sw.Elapsed.TotalMilliseconds, result);
    }


    private static void Validate(string name, long actual, long expected)
    {
        if (actual != expected)
        {
            throw new InvalidOperationException($"{name} produced wrong result: {actual} (expected {expected}).");
        }
    }

    private static void PrintRow(string name, double elapsedMs, int iterations)
    {
        Console.WriteLine($"  {name,-28} {elapsedMs,10:F2} ms total  | {elapsedMs / iterations,8:F3} ms/op");
    }

    private static long CountNodes(SmlDocument document)
    {
        static long CountNode(SmlNode node)
        {
            long total = 1;
            foreach (var child in node.Children)
            {
                total += CountNode(child);
            }
            return total;
        }

        long sum = 0;
        foreach (var root in document.Roots)
        {
            sum += CountNode(root);
        }
        return sum;
    }

    private static string BuildSmlDocument(int rows)
    {
        var writer = new System.Text.StringBuilder();
        writer.AppendLine("Window {");
        writer.AppendLine("    id: mainWindow");
        writer.AppendLine("    title: \"Perf\" ");
        writer.AppendLine("    VBox {");
        for (var i = 0; i < rows; i++)
        {
            writer.AppendLine($"        Label {{ id: row{i} text: \"Row {i}\" }}");
        }

        writer.AppendLine("    }");
        writer.AppendLine("}");
        return writer.ToString();
    }

    private static string BuildSmsScript(int loopCount)
    {
        return $$"""
        var n = {{loopCount}}
        var sum = 0
        for (var i = 0; i < n; i = i + 1) {
            sum = sum + i
        }
        sum
        """;
    }

}
