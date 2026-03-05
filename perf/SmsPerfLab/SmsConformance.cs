using Runtime.Sms;
using System.Globalization;

internal static class SmsConformance
{
    public static int Run(string fixturesDir)
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

            if (!NativeSmsBridge.TryExecute(source, out var native))
            {
                Console.WriteLine($"[FAIL] {label}");
                Console.WriteLine($"  Native execute error: {NativeSmsBridge.LastError}");
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

